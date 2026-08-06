using System;
using System.Net;
using Sqlbi.Bravo.Host;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Policies;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Bravo.Tests.Host;

/// <summary>
/// Fakes for the initialization entries. A fake rather than a real
/// <see cref="BravoApplicationInstance"/> is required: the real type always claims the production
/// named-pipe name, so constructing one in a test would risk colliding with an actually-running
/// Bravo instance.
/// </summary>
internal sealed class FakeInstance : IBravoApplicationInstance
{
    public bool IsPrimary { get; init; }

    public bool Disposed { get; private set; }

    public event EventHandler<InstanceActivationRequestedEventArgs>? ActivationRequested
    {
        add { }
        remove { }
    }

    public void RedirectActivationToPrimary()
    {
    }

    public void Dispose() => Disposed = true;
}

internal sealed class FakeTelemetry : ITelemetryService
{
    public bool Disposed { get; private set; }

    public bool TelemetryEnabled { get; set; }

    public void TrackException(Exception exception)
    {
    }

    public void Dispose() => Disposed = true;
}

internal sealed class FakeWebProxy : IWebProxy
{
    public ICredentials? Credentials { get; set; }

    public Uri? GetProxy(Uri destination) => null;

    public bool IsBypassed(Uri host) => true;
}

internal static class FakeInitializationContext
{
    /// <summary>
    /// Creates a <see cref="BravoApplicationInitializationContext"/> whose entries are all fakes.
    /// </summary>
    public static BravoApplicationInitializationContext Create(FakeInstance? instance = null)
    {
        return new BravoApplicationInitializationContext(
            instance ?? new FakeInstance());
    }
}
