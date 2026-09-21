using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using Sqlbi.Bravo.Infrastructure.Configuration;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Policies;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Host;

internal sealed partial class BootstrapContextFactory(
    ILoggerFactory loggerFactory,
    IPolicyServiceFactory policyFactory,
    IUserSettingsServiceFactory settingsFactory,
    ITelemetryServiceFactory telemetryFactory)
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IPolicyServiceFactory _policyFactory = policyFactory;
    private readonly IUserSettingsServiceFactory _settingsFactory = settingsFactory;
    private readonly ITelemetryServiceFactory _telemetryFactory = telemetryFactory;

    public static BootstrapContext CreateDefault()
    {
        var loggerFactory = CreateLoggerFactory();
        try
        {
            var factory = new BootstrapContextFactory(
                loggerFactory,
                new PolicyServiceFactory(),
                new UserSettingsServiceFactory(),
                new TelemetryServiceFactory());

            return factory.Create();
        }
        catch
        {
            loggerFactory.Dispose();
            throw;
        }
    }

    public BootstrapContext Create()
    {
        var policies = CreatePolicies();
        var settings = CreateSettings();

        var consent = TelemetryConsent.Resolve(policies.Current, settings);
        var telemetry = CreateTelemetry(consent.IsEnabled);

        return new BootstrapContext(_loggerFactory, policies, settings, telemetry);
    }

    private static ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create((builder) =>
        {
            builder.AddEventSourceLogger();
            builder.AddEventLog();
            builder.AddFilter<EventLogLoggerProvider>((level) => level >= LogLevel.Warning);
            builder.AddDiagnosticMessages();
            builder.AddFilter<DiagnosticMessageLoggerProvider>((level) => level >= LogLevel.Warning);
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
