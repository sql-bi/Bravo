using System;

namespace Bravo.Setup.Telemetry
{
    /// <summary>
    /// Reports what a package did, when who installs chose to send the telemetry of the installer. As in 1.0.x, the
    /// choice alone decides: the policies of an administrator are for the application.
    /// </summary>
    internal sealed class InstallationEventReporter
    {
        private readonly IInstallationEventSink _sink;
        private readonly SetupEnvironment _environment;

        public InstallationEventReporter(IInstallationEventSink sink, SetupEnvironment environment)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _sink = sink;
            _environment = environment;
        }

        public void Report(InstallationEventKind kind, InstalledProduct product, bool isTelemetryEnabled)
        {
            if (!isTelemetryEnabled)
            {
                return;
            }

            var userId = TelemetryUserId.Create(_environment.MachineName, _environment.UserName);
            _sink.Send(new InstallationEvent(GetName(kind), userId, _environment.OperatingSystem, product));
        }

        // The names of the events of 1.0.x: the counts of one version and of the other are compared by them.
        private static string GetName(InstallationEventKind kind)
        {
            switch (kind)
            {
                case InstallationEventKind.Install:
                    return "Install";
                case InstallationEventKind.Uninstall:
                    return "Uninstall";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, message: null);
            }
        }
    }
}
