using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bravo.Platform.Windows.Tests.Network;

/// <summary>
/// A proxy server of a test, on the loopback address. It forwards nothing: it answers every request itself,
/// after the challenge when it has one, and records what it was asked. A request that names no proxy is
/// answered in the same way, so it also stands for a server.
/// </summary>
internal sealed class TestProxy : IAsyncDisposable
{
    private const string AuthorizationHeader = "Proxy-Authorization:";

    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stopping = new();
    private readonly List<TestProxyRequest> _requests = [];
    private readonly Task _accepting;

    public TestProxy()
    {
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Address = new Uri($"http://127.0.0.1:{port}/");
        _accepting = Task.Run(AcceptAsync);
    }

    public Uri Address { get; }

    /// <summary>
    /// An address of the loopback where nothing listens: a connection there is refused.
    /// </summary>
    public static Uri ClosedAddress()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return new Uri($"http://127.0.0.1:{port}/");
    }

    /// <summary>
    /// The schemes that the proxy asks credentials with, such as <c>Basic realm="test"</c>. With none it asks
    /// for no credentials.
    /// </summary>
    public IReadOnlyList<string> Challenges { get; init; } = [];

    /// <summary>
    /// The credentials that end the challenge, sent with Basic. With none, nothing ends it.
    /// </summary>
    public NetworkCredential? Accepts { get; init; }

    /// <summary>
    /// The status that the proxy refuses a tunnel with. With none it says that the tunnel is open, and closes.
    /// </summary>
    public HttpStatusCode? TunnelStatus { get; init; }

    /// <summary>
    /// What the proxy answers a request that is not a tunnel with.
    /// </summary>
    public string Body { get; init; } = "answered by the proxy of the test";

    /// <summary>
    /// The media type of <see cref="Body"/>.
    /// </summary>
    public string ContentType { get; init; } = "text/plain; charset=utf-8";

    public IReadOnlyList<TestProxyRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return [.. _requests];
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync();
        _listener.Stop();
        await _accepting;
        _stopping.Dispose();
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
        catch (OperationCanceledException)
        {
            // The test has ended.
        }
        catch (SocketException)
        {
            // The listener was stopped while it accepted.
        }
    }

    // One request for each connection: the answer closes it.
    private async Task AnswerAsync(TcpClient client)
    {
        try
        {
            using (client)
            {
                var stream = client.GetStream();
                var head = await ReadHeadAsync(stream, _stopping.Token);
                if (head is null)
                {
                    return;
                }

                var response = Answer(head);
                await stream.WriteAsync(Encoding.UTF8.GetBytes(response), _stopping.Token);
            }
        }
        catch (Exception exception) when (exception is System.IO.IOException or OperationCanceledException or SocketException)
        {
            // The client went away.
        }
    }

    private string Answer(string head)
    {
        var lines = head.Split("\r\n");
        var requestLine = lines[0].Split(' ');
        var authorization = lines
            .FirstOrDefault(line => line.StartsWith(AuthorizationHeader, StringComparison.OrdinalIgnoreCase))?[AuthorizationHeader.Length..]
            .Trim();
        var request = new TestProxyRequest(requestLine[0], requestLine.ElementAtOrDefault(1) ?? string.Empty, authorization?.Split(' ')[0]);
        lock (_requests)
        {
            _requests.Add(request);
        }

        if (Challenges.Count > 0 && !IsAccepted(authorization))
        {
            var challenges = string.Concat(Challenges.Select(challenge => $"Proxy-Authenticate: {challenge}\r\n"));
            return Response("407 Proxy Authentication Required", string.Empty, challenges);
        }

        if (request.Method != "CONNECT")
        {
            return Response("200 OK", Body);
        }

        if (TunnelStatus is { } status)
        {
            return Response($"{(int)status} {status}", string.Empty);
        }

        return "HTTP/1.1 200 Connection established\r\n\r\n";
    }

    private bool IsAccepted(string? authorization)
    {
        if (Accepts is null || authorization is null)
        {
            return false;
        }

        var pair = Encoding.UTF8.GetBytes($"{Accepts.UserName}:{Accepts.Password}");

        return authorization == $"Basic {Convert.ToBase64String(pair)}";
    }

    private string Response(string status, string body, string headers = "")
    {
        var length = Encoding.UTF8.GetByteCount(body);

        return $"HTTP/1.1 {status}\r\nContent-Type: {ContentType}\r\nContent-Length: {length}\r\n{headers}Connection: close\r\n\r\n{body}";
    }

    private static async Task<string?> ReadHeadAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        var length = 0;
        while (length < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
            if (read == 0)
            {
                return null;
            }

            length += read;
            var text = Encoding.Latin1.GetString(buffer, 0, length);
            var end = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (end >= 0)
            {
                return text[..end];
            }
        }

        return null;
    }
}
