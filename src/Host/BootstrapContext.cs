using System;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
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
    private bool _disposed;

    internal BootstrapContext(
        ILoggerFactory loggerFactory,
        IPolicyService policyService,
        IUserSettings settings,
        ITelemetryService telemetry)
    {
        LoggerFactory = loggerFactory;
        PolicyService = policyService;
        Settings = settings;
        Telemetry = telemetry;
    }

    public ILoggerFactory LoggerFactory { get; }
    public IPolicyService PolicyService { get; }
    public IUserSettings Settings { get; }
    public ITelemetryService Telemetry { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        Telemetry.Dispose();
        // LoggerFactory is disposed last so all other services can safely log during their disposal.
        LoggerFactory.Dispose();
    }
}
