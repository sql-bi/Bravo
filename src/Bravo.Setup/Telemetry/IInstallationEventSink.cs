namespace Bravo.Setup.Telemetry;

/// <summary>
/// Where the events of the installer go. An installation waits for the program: an implementation returns
/// within a few seconds and never throws for an error of the network.
/// </summary>
internal interface IInstallationEventSink
{
    void Send(InstallationEvent installationEvent);
}
