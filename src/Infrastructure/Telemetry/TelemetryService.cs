namespace Sqlbi.Bravo.Infrastructure.Telemetry;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Sqlbi.Bravo.Infrastructure.Configuration;

public interface ITelemetryService : IDisposable
{
    bool TelemetryEnabled { get; set; }
    void TrackException(Exception exception);
}

internal sealed class TelemetryService : ITelemetryService
{
    private readonly TelemetryConfiguration _configuration;
    private readonly TelemetryClient _client;

    public TelemetryService()
    {
        _configuration = CreateConfiguration();
        _client = new TelemetryClient(_configuration);
    }

    public bool TelemetryEnabled
    {
        get => _configuration.DisableTelemetry == false;
        set => _configuration.DisableTelemetry = !value;
    }

    public void TrackException(Exception exception)
    {
        if (exception is AggregateException aex)
            exception = aex.GetBaseException();

        _client.TrackException(exception);
    }

    public void Dispose()
    {
        _client.Flush();
        _configuration.Dispose();
    }

    /// <summary>
    /// Tracks a fatal exception in scenarios where the DI container is unavailable.
    /// </summary>
    public static void TrackFatalException(Exception exception)
    {
        if (exception is AggregateException aex)
            exception = aex.GetBaseException();

        try
        {
            using var configuration = CreateConfiguration();
            var client = new TelemetryClient(configuration);
            client.TrackException(exception);
            client.Flush(); // Blocking flush is acceptable — app is terminating
        }
        catch
        {
            // Telemetry failure must not mask the original exception
        }
    }

    private static TelemetryConfiguration CreateConfiguration()
    {
        // Use parameterless constructor to avoid default processors (e.g. adaptive sampling)
        // that TelemetryConfiguration.CreateDefault() would register.
        var configuration = new TelemetryConfiguration();

        // Remark: ensure no sampling is applied — every exception must be recorded
        var processorChain = configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
        processorChain.Use((next) => new DefaultTelemetryProcessor(next));
        processorChain.Build();

        configuration.TelemetryInitializers.Add(new DefaultTelemetryInitializer());
        configuration.ConnectionString = TelemetrySessionInfo.ConnectionString;
        configuration.DisableTelemetry = UserPreferences.Current.TelemetryEnabled == false;
#if DEBUG
        configuration.TelemetryChannel.DeveloperMode = Debugger.IsAttached;
#endif
        return configuration;
    }
}
