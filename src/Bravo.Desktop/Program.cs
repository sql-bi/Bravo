using System;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Hosting;
using Bravo.Desktop.SingleInstance;
using Bravo.Platform.Windows.SingleInstance;

namespace Bravo.Desktop;

internal static class Program
{
    // Synchronous: an asynchronous entry point does not run on the STA thread that Windows Forms needs.
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var fatalExceptionHandler = new FatalExceptionDialog();
        LastChanceExceptionHandling.Install(fatalExceptionHandler);

        // Before the application reads or writes anything of its own: a second process ends in the arbitration.
        var request = ActivationCommandLine.Parse(args);
        var names = InstanceNames.ForCurrentUserSession(BravoInstance.ApplicationId);
        var options = new SingleInstanceOptions();
        var arbiter = new SingleInstanceArbiter(names, options, TimeProvider.System);
        var arbitration = arbiter.Arbitrate(ActivationPayload.Encode(request));
        if (!arbitration.IsOwner)
        {
            return SecondInstance.End(arbitration);
        }

        // Released last, after the host is disposed: until then no other process becomes the owner.
        using (arbitration.Ownership)
        {
            using var host = DesktopHost.Build(arbitration.Ownership, options, fatalExceptionHandler);
            DesktopHost.Run(host, request);
        }

        return ExitCode.Success;
    }
}
