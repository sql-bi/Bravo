namespace Bravo.Setup.Telemetry
{
    /// <summary>
    /// Sends nothing: the sink of a build that is not the one of the packages.
    /// </summary>
    internal sealed class NullInstallationEventSink : IInstallationEventSink
    {
        public void Send(InstallationEvent installationEvent)
        {
        }
    }
}
