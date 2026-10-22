using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Reports the exceptions that reach the global handlers.
/// </summary>
/// <remarks>
/// A fatal exception is saved as a crash report, which a reporter process shows and sends: the process that crashed
/// shows no dialog and sends nothing, unless the report cannot be saved or the reporter cannot start. An unobserved
/// task exception is logged and tracked, and the process keeps running.
/// </remarks>
internal sealed partial class UnhandledExceptionHandler(
    CrashReportStore store,
    ICrashReporterLauncher reporterLauncher,
    TimeProvider timeProvider,
    Action<Exception> showFallback)
{
    // Before the bootstrap, consent is unknown, so telemetry is not allowed and the report cannot be sent.
    private static readonly ReportingServices s_noServices = new(
        NullLogger.Instance,
        new NullTelemetryService(),
        new TelemetryConsent(IsEnabled: false, TelemetryConsentSource.UserSettings));

    private readonly CrashReportStore _store = store;
    private readonly ICrashReporterLauncher _reporterLauncher = reporterLauncher;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly Action<Exception> _showFallback = showFallback;
    private volatile ReportingServices _services = s_noServices;

    /// <summary>
    /// Reports through the given services until the returned object is disposed.
    /// </summary>
    /// <param name="loggerFactory">Creates the logger of the handler.</param>
    /// <param name="telemetry">
    /// Tracks unobserved task exceptions, and tells the session and whether the user still allows telemetry.
    /// </param>
    /// <param name="consent">The consent resolved at startup, which a crash report records.</param>
    public IDisposable AttachServices(ILoggerFactory loggerFactory, ITelemetryService telemetry, TelemetryConsent consent)
    {
        var logger = loggerFactory.CreateLogger<UnhandledExceptionHandler>();
        var services = new ReportingServices(logger, telemetry, consent);
        _services = services;

        return new ServicesAttachment(this, services);
    }

    public void Handle(Exception exception, ExceptionOrigin origin)
    {
        var services = _services;

        if (origin == ExceptionOrigin.UnobservedTask)
        {
            Log(services, exception, origin);
            Track(services, exception, origin);
            return;
        }

        // The report first: logging can fail for the same reason the process crashed.
        ReportCrash(services, exception);
        Log(services, exception, origin);
    }

    private void ReportCrash(ReportingServices services, Exception exception)
    {
        try
        {
            var telemetryAllowed = IsTelemetryAllowed(services);
            var redactor = UserProfilePathRedactor.CreateForCurrentUser();
            var report = CrashReport.Create(exception, services.Telemetry.SessionId, telemetryAllowed, _timeProvider.GetUtcNow(), redactor);

            var reportPath = _store.Save(report);
            _reporterLauncher.Launch(reportPath);
        }
        catch (Exception reportException)
        {
            try
            {
                LogCrashReportFailed(services.Logger, reportException);
            }
            catch { /* Ignore failures */ }

            // Without a reporter, this process tracks the crash itself, when telemetry allows it.
            Track(services, exception, ExceptionOrigin.AppDomain);
            try
            {
                _ = services.Telemetry.TryFlushBeforeShutdown(3_000);
            }
            catch { /* Ignore failures */ }

            try
            {
                _showFallback(exception);
            }
            catch { /* Ignore failures */ }
        }
    }

    // A policy cannot change while Bravo runs, but the user can turn telemetry on or off in the options.
    private static bool IsTelemetryAllowed(ReportingServices services)
    {
        if (services.Consent.Source == TelemetryConsentSource.Policy)
        {
            return services.Consent.IsEnabled;
        }

        return services.Telemetry.TelemetryEnabled;
    }

    private static void Log(ReportingServices services, Exception exception, ExceptionOrigin origin)
    {
        try
        {
            LogUnhandledException(services.Logger, exception, origin);
        }
        catch { /* Ignore failures */ }
    }

    private static void Track(ReportingServices services, Exception exception, ExceptionOrigin origin)
    {
        try
        {
            services.Telemetry.TrackException(exception, CreateTelemetryProperties(origin));
        }
        catch { /* Ignore failures */ }
    }

    /// <summary>
    /// The properties of an unhandled exception in telemetry, which queries filter on.
    /// </summary>
    internal static Dictionary<string, string> CreateTelemetryProperties(ExceptionOrigin origin)
    {
        return new Dictionary<string, string>
        {
            ["ExceptionOrigin"] = origin.ToString(),
            ["ExceptionKind"] = "Unhandled",
        };
    }

    private sealed record ReportingServices(
        ILogger Logger,
        ITelemetryService Telemetry,
        TelemetryConsent Consent);

    private sealed class ServicesAttachment(UnhandledExceptionHandler owner, ReportingServices services) : IDisposable
    {
        private readonly UnhandledExceptionHandler _owner = owner;
        private readonly ReportingServices _services = services;

        // Detaches only its own services, which a later attachment may have replaced.
        public void Dispose() => Interlocked.CompareExchange(ref _owner._services, s_noServices, _services);
    }
}
