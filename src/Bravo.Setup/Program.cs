using System;
using Bravo.Setup.ExternalTools;
using Bravo.Setup.Telemetry;
using Bravo.Setup.UserData;

namespace Bravo.Setup;

internal static class Program
{
    private static int Main(string[] args)
    {
        var reporter = new InstallationEventReporter(new NullInstallationEventSink(), SetupEnvironment.Read());
        var application = new SetupApplication(
            new ExternalToolRegistration(),
            reporter,
            new UserDataFolder(),
            Console.Error);

        return application.Run(args);
    }
}
