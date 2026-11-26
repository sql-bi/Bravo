using System;
using Bravo.Setup.ExternalTools;
using Bravo.Setup.Telemetry;
using Bravo.Setup.UserData;

namespace Bravo.Setup;

internal static class Program
{
    private static int Main(string[] args)
    {
        return ProcessBoundary.Run(() => Compose().Run(args), Console.Error);
    }

    private static SetupApplication Compose()
    {
#if BRAVO_RELEASE_BUILD
        // Only the build of the packages sends, to the resource of the application.
        var destination = TelemetryDestination.Parse(TelemetryDestination.ReleaseConnectionString);
        IInstallationEventSink sink = new HttpInstallationEventSink(
            destination, TimeProvider.System, HttpInstallationEventSink.DefaultTimeout);
#else
        IInstallationEventSink sink = new NullInstallationEventSink();
#endif
        var reporter = new InstallationEventReporter(sink, SetupEnvironment.Read());
        return new SetupApplication(new ExternalToolRegistration(), reporter, new UserDataFolder(), Console.Error);
    }
}
