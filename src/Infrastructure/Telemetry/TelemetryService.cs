namespace Sqlbi.Bravo.Infrastructure.Telemetry;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Sqlbi.Bravo.Infrastructure.Configuration;
using Sqlbi.Bravo.Infrastructure.Policies;

public interface ITelemetryService : IDisposable
{
    bool TelemetryEnabled { get; set; }
    void TrackException(Exception exception);
}

internal sealed class TelemetryService : ITelemetryService
{
    private readonly TelemetryConfiguration _configuration;
    private readonly TelemetryClient _client;
    private readonly IPolicies _policies;

    private static readonly Lazy<TelemetryService> _instance = new(CreateInstance, isThreadSafe: true);

    public static TelemetryService Instance => _instance.Value;

    private static TelemetryService CreateInstance()
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

        var policies = PoliciesFactory.Create();

        // Determine whether telemetry is enabled based on policies and user settings.
        var telemetryEnabled = policies.TelemetryEnabled ?? UserPreferences.Current.TelemetryEnabled;
        configuration.DisableTelemetry = !telemetryEnabled;
#if DEBUG
        configuration.TelemetryChannel.DeveloperMode = Debugger.IsAttached;
#endif
        return new TelemetryService(configuration, policies);
    }

    private TelemetryService(TelemetryConfiguration configuration, IPolicies policies)
    {
        _configuration = configuration;
        _client = new TelemetryClient(configuration);
        _policies = policies;
    }

    public bool TelemetryEnabled
    {
        get => !_configuration.DisableTelemetry;
        set
        {
            var telemetryEnabled = _policies.TelemetryEnabled ?? value;
             _configuration.DisableTelemetry = !telemetryEnabled;
        }
    }

    public void TrackException(Exception exception)
    {
        if (exception is AggregateException aex)
            exception = aex.GetBaseException();

        _client.TrackException(exception);
        _client.Flush();
    }

    public void Dispose()
    {
        _configuration.Dispose();
    }
}
