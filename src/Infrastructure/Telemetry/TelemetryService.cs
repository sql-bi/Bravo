using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;

namespace Sqlbi.Bravo.Infrastructure.Telemetry;

public interface ITelemetryService : IDisposable
{
    bool TelemetryEnabled { get; set; }
    void TrackException(Exception exception, IDictionary<string, string>? properties = null);
    Task<bool> FlushAsync(CancellationToken cancellationToken = default);
    bool TryFlushBeforeShutdown(int timeoutMilliseconds);
}

internal sealed class TelemetryService : ITelemetryService
{
    public static readonly string DefaultStorageFolder =
        Path.Combine(AppEnvironment.ApplicationCachePath, ".telemetry");

    private readonly TelemetryConfiguration _configuration;
    private readonly ITelemetryChannel _channel;
    private readonly TelemetryClient _client;
    private bool _disposed;

    private TelemetryService(TelemetryConfiguration configuration)
    {
        _configuration = configuration;
        _channel = configuration.TelemetryChannel;
        _client = new TelemetryClient(configuration);
    }

    public bool TelemetryEnabled
    {
        get => _configuration.DisableTelemetry == false;
        set => _configuration.DisableTelemetry = !value;
    }

    public void TrackException(Exception exception, IDictionary<string, string>? properties = null)
    {
        _client.TrackException(exception, properties);
        // With ServerTelemetryChannel, Flush moves telemetry out of the in-memory telemetry
        // buffer and into the transmission pipeline without waiting for delivery. Calling it
        // after TrackException improves the chances of preserving the exception if the process
        // terminates before the next scheduled flush, while keeping this path non-blocking.
        // It does not guarantee that the telemetry has been sent or persisted to local storage.
        _client.Flush(); 
    }

    public Task<bool> FlushAsync(CancellationToken cancellationToken = default)
    {
        return _client.FlushAsync(cancellationToken);
    }

    public bool TryFlushBeforeShutdown(int timeoutMilliseconds)
    {
        // At shutdown, wait for ServerTelemetryChannel to flush telemetry that could
        // otherwise be lost when the process exits. FlushAsync completes once pending
        // telemetry has been sent or persisted to local storage. The wait is bounded
        // because telemetry must never prevent the application from terminating.
        using var cts = new CancellationTokenSource(timeoutMilliseconds);
        try
        {
            return _client.FlushAsync(cts.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _ = TryFlushBeforeShutdown(5_000);

        _configuration.Dispose();

        // A channel assigned to the sink is not owned by it.
        _channel.Dispose();
    }

    public static TelemetryService Create(string connectionString, bool telemetryEnabled, string storageFolder)
    {
        var channel = new ServerTelemetryChannel
        {
            // Remark: folder must exist beforehand as the channel does not auto-create it.
            // If missing, local storage remains disabled and unsent telemetry is lost.
            StorageFolder = storageFolder
        };

#if DEBUG
        channel.DeveloperMode = Debugger.IsAttached;
#endif

        // Avoid CreateDefault() to prevent registering default processors (e.g., adaptive sampling)
        var configuration = new TelemetryConfiguration
        {
            ConnectionString = connectionString,
            DisableTelemetry = !telemetryEnabled,
            TelemetryChannel = channel
        };

        configuration.TelemetryInitializers.Add(new DefaultTelemetryInitializer());

        // Build clean processor chain with DefaultTelemetryProcessor (disables sampling/filtering).
        var processorChain = configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
        processorChain.Use(next => new DefaultTelemetryProcessor(next));
        processorChain.Build();

        channel.Initialize(configuration);

        return new TelemetryService(configuration);
    }
}
