using System;
using Bravo.Core.Policies;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Hosting;
using Bravo.Desktop.Policies;
using Bravo.Desktop.SingleInstance;
using Bravo.Platform.Windows.Policies;
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

        // Read once: a development build is another instance than the installed application.
        var identity = BuildIdentity.FromAssembly(typeof(Program).Assembly);

        // Before the application reads or writes anything of its own: a second process ends in the arbitration.
        var start = ActivationCommandLine.Parse(args, identity);
        var names = InstanceNames.ForCurrentUserSession(identity.ApplicationId);
        var options = new SingleInstanceOptions();
        var arbiter = new SingleInstanceArbiter(names, options, TimeProvider.System);
        var arbitration = arbiter.Arbitrate(ActivationPayload.Encode(start.Request));
        if (!arbitration.IsOwner)
        {
            return SecondInstance.End(arbitration);
        }

        var dataFolder = DataFolder.For(identity, start.Development);

        // Released last, after the host is disposed: until then no other process becomes the owner.
        using (arbitration.Ownership)
        {
            // Read once: a change made while the application runs applies at its next start.
            ApplicationPolicies policies;
            try
            {
                policies = RegistryPolicyReader.Read(PolicyKeys.For(identity, start.Development));
            }
            catch (PolicyReadException exception)
            {
                return PolicyReadFailureDialog.End(exception);
            }

            using var host = DesktopHost.Build(identity, dataFolder, start.Development, policies, arbitration.Ownership, options, fatalExceptionHandler);
            return DesktopHost.Run(host, start.Request);
        }
    }
}
