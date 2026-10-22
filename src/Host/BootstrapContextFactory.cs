using System;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Configuration;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Policies;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Host;

internal sealed partial class BootstrapContextFactory(
    ILoggerFactory loggerFactory,
    DiagnosticLevelSwitch diagnosticLevel,
    DiagnosticLogStore diagnosticLog,
    IPolicyServiceFactory policyFactory,
    IUserSettingsServiceFactory settingsFactory,
    ITelemetryServiceFactory telemetryFactory,
    UnhandledExceptionHandler exceptionHandler)
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly DiagnosticLevelSwitch _diagnosticLevel = diagnosticLevel;
    private readonly DiagnosticLogStore _diagnosticLog = diagnosticLog;
    private readonly IPolicyServiceFactory _policyFactory = policyFactory;
    private readonly IUserSettingsServiceFactory _settingsFactory = settingsFactory;
    private readonly ITelemetryServiceFactory _telemetryFactory = telemetryFactory;
    private readonly UnhandledExceptionHandler _exceptionHandler = exceptionHandler;

    public static BootstrapContext CreateDefault(UnhandledExceptionHandler exceptionHandler)
    {
        var diagnosticLevel = new DiagnosticLevelSwitch();
        var diagnosticLog = new DiagnosticLogStore();
        var loggerFactory = CreateLoggerFactory(diagnosticLevel, diagnosticLog);
        try
        {
            var factory = new BootstrapContextFactory(
                loggerFactory,
                diagnosticLevel,
                diagnosticLog,
                new PolicyServiceFactory(),
                new UserSettingsServiceFactory(),
                new TelemetryServiceFactory(),
                exceptionHandler);

            return factory.Create();
        }
        catch
        {
            loggerFactory.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates the bootstrap services and attaches them to the exception handler, which reports through them until
    /// the context is disposed.
    /// </summary>
    public BootstrapContext Create()
    {
        var policies = CreatePolicies();
        var settings = CreateSettings();
        _diagnosticLevel.Level = settings.DiagnosticLevel;

        var consent = TelemetryConsent.Resolve(policies.Current, settings);
        var telemetry = CreateTelemetry(consent.IsEnabled);
        var exceptionHandlerAttachment = _exceptionHandler.AttachServices(_loggerFactory, telemetry, consent);

        return new BootstrapContext(_loggerFactory, _diagnosticLevel, _diagnosticLog, policies, settings, telemetry, exceptionHandlerAttachment);
    }

    private static ILoggerFactory CreateLoggerFactory(DiagnosticLevelSwitch diagnosticLevel, DiagnosticLogStore diagnosticLog)
    {
        return LoggerFactory.Create((builder) =>
        {
            builder.AddEventSourceLogger();
            builder.AddBravoEventLog();
            builder.AddBravoDiagnosticLog(diagnosticLog, diagnosticLevel, TimeProvider.System);
#if DEBUG
            builder.AddConsole();
            builder.AddDebug();
#endif
        });
    }

    /// <remarks>
    /// Policy initialization failures must halt the application startup
    /// to prevent bypassing administrative restrictions.
    /// </remarks>
    private IPolicyService CreatePolicies()
    {
        var factory = _policyFactory.Create();

        // Force instantiation to detect any potential exceptions
        _ = factory.Current;

        return factory;
    }

    private IUserSettings CreateSettings()
    {
        try
        {
            return _settingsFactory.Create();
        }
        catch (Exception ex)
        {
            LogUserSettingsCreationFailed(_loggerFactory.CreateLogger<BootstrapContextFactory>(), ex);
            return new UserSettings();
        }
    }

    private ITelemetryService CreateTelemetry(bool enabled)
    {
        try
        {
            return _telemetryFactory.Create(enabled);
        }
        catch (Exception ex)
        {
            LogTelemetryCreationFailed(_loggerFactory.CreateLogger<BootstrapContextFactory>(), ex);
            return new NullTelemetryService();
        }
    }
}
