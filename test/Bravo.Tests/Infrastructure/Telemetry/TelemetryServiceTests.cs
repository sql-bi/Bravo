using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Xunit;

namespace Bravo.Tests.Infrastructure.Telemetry;

/// <summary>
/// Exercises the real channel against a local ingestion endpoint: the behaviour worth protecting is
/// what reaches the wire before the process exits, and it only exists in the channel itself.
/// </summary>
public class TelemetryServiceTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    private static string CreateConnectionString(int port)
        => $"InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:{port}/";

    private static int GetFreePort()
    {
        using var socket = new System.Net.Sockets.TcpListener(IPAddress.Loopback, port: 0);
        socket.Start();
        return ((IPEndPoint)socket.LocalEndpoint).Port;
    }

    /// <summary>
    /// Regression: Dispose used the synchronous Flush, which hands the buffer to the sender and
    /// returns before the request is sent, so the last exceptions of a session never left the process.
    /// </summary>
    [Fact]
    public void Dispose_TrackedExceptionsAreSentBeforeReturning()
    {
        var port = GetFreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        using var received = new ManualResetEventSlim();
        string? path = null;
        _ = Task.Run(() =>
        {
            var context = listener.GetContext();
            path = context.Request.Url?.AbsolutePath;
            // Set before the response is sent: Dispose can only return after the response.
            received.Set();
            context.Response.StatusCode = 200;
            context.Response.Close();
        });

        using var storage = new TemporaryFolder();
        var service = TelemetryService.Create(CreateConnectionString(port), telemetryEnabled: true, storage.Path);
        service.TrackException(new InvalidOperationException("tracked before exit"));

        service.Dispose();

        // Checked without waiting: the process exits right after Dispose, so a request that is
        // still in flight at this point is lost.
        Assert.True(received.IsSet, "The request had not reached the endpoint when Dispose returned.");
        Assert.Contains("/track", path);
    }

    /// <summary>
    /// With no endpoint the flush must fail fast and store the transmission for the next start,
    /// instead of holding the shutdown for the whole TCP timeout.
    /// </summary>
    [Fact]
    public void Dispose_EndpointUnreachable_StoresTheTransmissionAndReturnsWithinTheBound()
    {
        using var storage = new TemporaryFolder();
        var service = TelemetryService.Create(CreateConnectionString(GetFreePort()), telemetryEnabled: true, storage.Path);
        service.TrackException(new InvalidOperationException("tracked while offline"));

        var stopwatch = Stopwatch.StartNew();
        service.Dispose();

        Assert.True(stopwatch.Elapsed < s_timeout, $"Dispose took {stopwatch.Elapsed}");
        Assert.NotEmpty(Directory.GetFiles(storage.Path, "*.trn"));
    }

    private sealed class TemporaryFolder : IDisposable
    {
        public string Path { get; } = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Bravo.Tests.{Guid.NewGuid():N}")).FullName;

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // A file still held by the channel is left behind.
            }
        }
    }
}
