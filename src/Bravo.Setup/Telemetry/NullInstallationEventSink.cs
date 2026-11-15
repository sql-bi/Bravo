namespace Bravo.Setup.Telemetry;

/// <summary>
/// Sends nothing: the events of the installer reach no service yet.
/// </summary>
internal sealed class NullInstallationEventSink : IInstallationEventSink
{
    public void Send(InstallationEvent installationEvent)
    {
    }
}
