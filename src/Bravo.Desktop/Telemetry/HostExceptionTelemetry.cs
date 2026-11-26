using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Policies;
using Bravo.Core.Settings;
using Bravo.Core.Telemetry;
using Bravo.Desktop.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bravo.Desktop.Telemetry;

/// <summary>
/// Sends the exceptions of the host that nothing foresaw, while a policy or the user allows it: the pipeline
/// exists only then. When the user turns telemetry off, what waits in the store is deleted with it.
/// </summary>
internal sealed partial class HostExceptionTelemetry : IExceptionTelemetry, IHostedService, IDisposable
{
    // Within the stop of the host, which takes at most 5 seconds.
    private static readonly TimeSpan s_flushTimeout = TimeSpan.FromSeconds(3);

    private readonly TelemetryOptions _options;
    private readonly Func<ITelemetryPipeline> _createPipeline;
    private readonly TelemetryContext _context;
    private readonly ApplicationPolicies _policies;
    private readonly IUserSettingsStore _settings;
    private readonly ILogger<HostExceptionTelemetry> _logger;

    private readonly Lazy<PersonalNames> _personalNames;

    private readonly Lock _gate = new();
    private bool? _isOn;
    private bool _isStopped;
    private ITelemetryPipeline? _pipeline;

    /// <param name="options">Where the telemetry goes, and its store.</param>
    /// <param name="createPipeline">
    /// Creates the pipeline, on the store of the options, each time telemetry turns on with a destination.
    /// </param>
    /// <param name="context">What every item of the run carries.</param>
    /// <param name="policies">What an administrator set, which applies over the option of the user.</param>
    /// <param name="settings">The settings of the user, whose option is read again when they change.</param>
    /// <param name="personalNames">
    /// What the texts of an exception lose, read at the first one: Windows is not asked for them at every start.
    /// </param>
    /// <param name="logger">The log of the host.</param>
    public HostExceptionTelemetry(
        TelemetryOptions options,
        Func<ITelemetryPipeline> createPipeline,
        TelemetryContext context,
        ApplicationPolicies policies,
        IUserSettingsStore settings,
        Func<PersonalNames> personalNames,
        ILogger<HostExceptionTelemetry> logger)
    {
        _options = options;
        _createPipeline = createPipeline;
        _context = context;
        _policies = policies;
        _settings = settings;
        _personalNames = new Lazy<PersonalNames>(personalNames);
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.ConnectionString is null)
        {
            LogNoDestination();
            return Task.CompletedTask;
        }

        _settings.Changed += OnSettingsChanged;
        Apply();

        return Task.CompletedTask;
    }

    public void Track(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        try
        {
            if (Volatile.Read(ref _pipeline) is null)
            {
                return;
            }

            // Built outside the gate: the symbols and the scan of the texts would hold up a change of the settings
            // and the stop.
            var chain = ExceptionChain.Create(exception, _personalNames.Value);
            var item = ExceptionTelemetryFactory.Create(chain, _context, HostLogger.GetTraceId(Activity.Current));
            lock (_gate)
            {
                _pipeline?.Send(item);
            }
        }
        catch (Exception failure)
        {
            LogNotSent(failure);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Stop() is { } pipeline)
        {
            await FlushAsync(pipeline, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        // A host whose start failed is disposed without a stop: what the channel holds is kept all the same. On the
        // thread pool, as the stop of the host runs: the thread that disposes may have a message loop.
        if (Stop() is { } pipeline)
        {
            Task.Run(() => FlushAsync(pipeline, CancellationToken.None)).GetAwaiter().GetResult();
        }

        lock (_gate)
        {
            _pipeline?.Dispose();
            _pipeline = null;
        }
    }

    // A change of the settings that is still running no longer turns the pipeline off while it is flushed. Gives
    // the pipeline to flush, the first time only.
    private ITelemetryPipeline? Stop()
    {
        _settings.Changed -= OnSettingsChanged;

        lock (_gate)
        {
            if (_isStopped)
            {
                return null;
            }

            _isStopped = true;
            return _pipeline;
        }
    }

    // What the channel holds is sent, or kept for the next start: on .NET it keeps nothing when the process ends.
    // The flush of the client empties only its buffer; this one also keeps what the channel could not send yet.
    private async Task FlushAsync(ITelemetryPipeline pipeline, CancellationToken cancellationToken)
    {
        if (!await pipeline.FlushAsync(s_flushTimeout, cancellationToken).ConfigureAwait(false))
        {
            LogNotKept();
        }
    }

    // On the thread that replaced the settings: the change applies before the answer to the page.
    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        Apply();
    }

    private void Apply()
    {
        var consent = TelemetryConsent.Of(_policies, _settings.Current);
        lock (_gate)
        {
            if (_isStopped || consent.IsGiven == _isOn)
            {
                return;
            }

            _isOn = consent.IsGiven;
            if (consent.IsGiven)
            {
                TurnOn(consent.Source);
            }
            else
            {
                TurnOff(consent.Source);
            }
        }
    }

    // What the store kept from an earlier run goes with the new pipeline.
    private void TurnOn(TelemetryConsentSource source)
    {
        try
        {
            _pipeline = _createPipeline();
            LogOn(source);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogNotStarted(exception, _options.StorageFolder);
        }
    }

    // Nothing is sent from now on, not even what waits.
    private void TurnOff(TelemetryConsentSource source)
    {
        _pipeline?.Dispose();
        _pipeline = null;
        LogOff(source);

        try
        {
            TelemetryPipeline.DeleteStore(_options.StorageFolder);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogStoreNotDeleted(exception, _options.StorageFolder);
        }
    }
}
