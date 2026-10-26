using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Tests.Infrastructure.Diagnostics.CrashReporting;
using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;
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

    // The channel compresses what it sends.
    private static string ReadBody(HttpListenerRequest request)
    {
        var body = request.InputStream;
        if (string.Equals(request.Headers["Content-Encoding"], "gzip", StringComparison.OrdinalIgnoreCase))
        {
            body = new GZipStream(body, CompressionMode.Decompress);
        }

        using var reader = new StreamReader(body);
        return reader.ReadToEnd();
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
    /// Regression: the crash reporter tracks telemetry rebuilt from a saved report, which a processor rejected, so
    /// nothing was sent.
    /// </summary>
    [Fact]
    public async Task FlushAsync_TelemetryOfACrashReport_ReachesTheEndpoint()
    {
        var port = GetFreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        using var received = new ManualResetEventSlim();
        _ = Task.Run(() =>
        {
            var context = listener.GetContext();
            received.Set();
            context.Response.StatusCode = 200;
            context.Response.Close();
        });

        using var storage = new TemporaryFolder();
        using var service = TelemetryService.Create(CreateConnectionString(port), telemetryEnabled: true, storage.Path);
        service.TrackException(CrashReportTelemetry.Create(TestCrashReports.Create(DateTimeOffset.UtcNow)));

        using var timeout = new CancellationTokenSource(s_timeout);
        var sent = await service.FlushAsync(timeout.Token);

        Assert.True(sent);
        Assert.True(received.Wait(s_timeout), "The request did not reach the endpoint.");
    }

    [Fact]
    public async Task FlushAsync_ExceptionWithAUserProfileFolder_SendsTheFolderRedacted()
    {
        var port = GetFreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var body = Task.Run(() =>
        {
            var context = listener.GetContext();
            var text = ReadBody(context.Request);
            context.Response.StatusCode = 200;
            context.Response.Close();
            return text;
        });

        using var storage = new TemporaryFolder();
        using var service = TelemetryService.Create(CreateConnectionString(port), telemetryEnabled: true, storage.Path);
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        service.TrackException(new InvalidOperationException($@"Cannot read '{folder}\Bravo\settings.json'."));

        using var timeout = new CancellationTokenSource(s_timeout);
        await service.FlushAsync(timeout.Token);

        Assert.Contains(@"%LOCALAPPDATA%\\Bravo\\settings.json", await body.WaitAsync(s_timeout));
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
}
