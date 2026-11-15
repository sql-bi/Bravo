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
        var reporter = new InstallationEventReporter(new NullInstallationEventSink(), SetupEnvironment.Read());
        return new SetupApplication(new ExternalToolRegistration(), reporter, new UserDataFolder(), Console.Error);
    }
}
