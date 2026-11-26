using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Bravo.Desktop.Tests.Telemetry;

/// <summary>
/// The ingestion endpoint of Application Insights, as a test stands for it on the loopback address: it accepts
/// every item and keeps the bodies as they arrived, decompressed. No item of a test reaches a real resource.
/// </summary>
internal sealed class TestIngestion : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _stopping = new();
    private readonly List<string> _bodies = [];
    private readonly SemaphoreSlim _received = new(0);
    private readonly Task _accepting;

    /// <param name="port">The port on the loopback address, or 0 for any that is free.</param>
    public TestIngestion(int port = 0)
    {
        _listener = new TcpListener(IPAddress.Loopback, port);
        _listener.Start();
        var boundPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Origin = $"http://127.0.0.1:{boundPort}";
        ConnectionString = ConnectionStringFor(boundPort);
        _accepting = Task.Run(AcceptAsync);
    }

    /// <summary>
    /// The origin of the endpoint, as a connection string and <c>--dev-telemetry-endpoint</c> name it.
    /// </summary>
    public string Origin { get; }

    /// <summary>
    /// The connection string of a resource whose ingestion is this endpoint.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// The connection string of a resource of no one whose ingestion is the port of the loopback address.
    /// </summary>
    public static string ConnectionStringFor(int port)
    {
        return $"InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:{port}";
    }

    /// <summary>
    /// The bodies of the requests, decompressed, in the order they arrived.
    /// </summary>
    public IReadOnlyList<string> Bodies
    {
        get
        {
            lock (_bodies)
            {
                return [.. _bodies];
            }
        }
    }

    /// <summary>
    /// The items of every body: a body has one JSON object on each line.
    /// </summary>
    public IReadOnlyList<JsonElement> Items =>
    [
        .. Bodies
            .SelectMany(body => body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            .Select(line => JsonDocument.Parse(line).RootElement),
    ];

    /// <summary>
    /// Waits until the endpoint has received at least <paramref name="count"/> items.
    /// </summary>
    public async Task<IReadOnlyList<JsonElement>> WaitForItemsAsync(int count)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        while (Items.Count < count)
        {
            await _received.WaitAsync(timeout.Token);
        }

        return Items;
    }

    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync();
        _listener.Stop();
        await _accepting;
        _stopping.Dispose();
        _received.Dispose();
    }

    private async Task AcceptAsync()
    {
        try
        {
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync(_stopping.Token);
                _ = Task.Run(() => AnswerAsync(client));
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or SocketException or ObjectDisposedException or InvalidOperationException)
        {
            // The test has ended, and the listener was stopped.
        }
    }

    // Each request of the connection, until the client closes it.
    private async Task AnswerAsync(TcpClient client)
    {
        try
        {
            using (client)
            {
                var stream = client.GetStream();
                while (await ReadRequestAsync(stream, _stopping.Token) is { } body)
                {
                    lock (_bodies)
                    {
                        _bodies.Add(body);
                    }

                    _received.Release();
                    var items = body.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
                    var answer = $$"""{"itemsReceived":{{items}},"itemsAccepted":{{items}},"errors":[]}""";
                    var response = $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {answer.Length}\r\n\r\n{answer}";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(response), _stopping.Token);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException or SocketException or ObjectDisposedException)
        {
            // The client went away, or the test has ended.
        }
    }

    // The body of the next request, or null when the client closed the connection.
    private static async Task<string?> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var head = new List<byte>();
        var one = new byte[1];
        while (!EndsWithBlankLine(head))
        {
            if (await stream.ReadAsync(one, cancellationToken) == 0)
            {
                return null;
            }

            head.Add(one[0]);
        }

        var lines = Encoding.ASCII.GetString([.. head]).Split("\r\n");
        var length = int.Parse(Header(lines, "Content-Length") ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        var body = new byte[length];
        await stream.ReadExactlyAsync(body, cancellationToken);

        if (Header(lines, "Content-Encoding") == "gzip")
        {
            using var gzip = new GZipStream(new MemoryStream(body), CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            return await reader.ReadToEndAsync(cancellationToken);
        }

        return Encoding.UTF8.GetString(body);
    }

    private static bool EndsWithBlankLine(List<byte> head)
    {
        return head.Count >= 4 && head[^4] == '\r' && head[^3] == '\n' && head[^2] == '\r' && head[^1] == '\n';
    }

    private static string? Header(string[] lines, string name)
    {
        var prefix = name + ":";

        return lines.FirstOrDefault(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))?[prefix.Length..].Trim();
    }
}
