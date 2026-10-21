using System;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Policies;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Holds the services used to bootstrap the application.
/// </summary>
/// <remarks>
/// The context owns the lifetime of the services it holds.
/// </remarks>
internal sealed class BootstrapContext : IDisposable
{
    private readonly IDisposable? _exceptionHandlerAttachment;
    private bool _disposed;

    internal BootstrapContext(
        ILoggerFactory loggerFactory,
        DiagnosticLevelSwitch diagnosticLevel,
        DiagnosticLogStore diagnosticLog,
        IPolicyService policyService,
        IUserSettings settings,
        ITelemetryService telemetry,
        IDisposable? exceptionHandlerAttachment = null)
    {
        LoggerFactory = loggerFactory;
        DiagnosticLevel = diagnosticLevel;
        DiagnosticLog = diagnosticLog;
        PolicyService = policyService;
        Settings = settings;
        Telemetry = telemetry;
        _exceptionHandlerAttachment = exceptionHandlerAttachment;
    }

    public ILoggerFactory LoggerFactory { get; }
    public DiagnosticLevelSwitch DiagnosticLevel { get; }
    public DiagnosticLogStore DiagnosticLog { get; }
    public IPolicyService PolicyService { get; }
    public IUserSettings Settings { get; }
    public ITelemetryService Telemetry { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Detached first, so that an exception raised while the services are disposed is not reported through them.
        _exceptionHandlerAttachment?.Dispose();
        Telemetry.Dispose();
        // LoggerFactory is disposed last so all other services can safely log during their disposal.
        LoggerFactory.Dispose();
    }
}
