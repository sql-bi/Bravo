using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Bravo.Core.Policies;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Hosting;
using Bravo.Desktop.Policies;
using Bravo.Desktop.SingleInstance;
using Bravo.Platform.Windows.Policies;
using Bravo.Platform.Windows.SingleInstance;
using Microsoft.Extensions.Logging;

namespace Bravo.Desktop;

internal static partial class Program
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
            // The records of the start share a trace, the one of a log file that cannot be created included. It
            // ends before the host starts, whose requests and services have traces of their own.
            using var startActivity = new Activity("Bravo.Start").Start();

            // Closed after the host: what the host writes while it stops is recorded.
            using var log = HostLog.Open(dataFolder, writeToEventLog: !identity.IsDevelopmentBuild, TimeProvider.System);
            var logger = log.Factory.CreateLogger(typeof(Program).FullName!);
            LogStarted(logger, identity, log.FilePath);

            // Read once: a change made while the application runs applies at its next start.
            ApplicationPolicies policies;
            try
            {
                policies = RegistryPolicyReader.Read(PolicyKeys.For(identity, start.Development));
            }
            catch (PolicyReadException exception)
            {
                LogPoliciesUnreadable(logger, exception);

                // Released before the message: a start meanwhile reads the policies itself, and is not handed
                // over to a process that has no host to take it.
                arbitration.Ownership.Dispose();
                return PolicyReadFailureDialog.End(exception);
            }

            using var host = DesktopHost.Build(identity, dataFolder, start.Development, policies, arbitration.Ownership, options, fatalExceptionHandler, log);
            startActivity.Stop();

            var exitCode = DesktopHost.Run(host, start.Request);
            LogEnded(logger, exitCode);

            return exitCode;
        }
    }

    private static void LogStarted(ILogger logger, BuildIdentity identity, string? logFile)
    {
        LogStarted(
            logger,
            identity.Version,
            identity.IsDevelopmentBuild,
            RuntimeInformation.FrameworkDescription,
            identity.IncludesRuntime,
            RuntimeInformation.ProcessArchitecture,
            RuntimeInformation.OSDescription,
            logFile ?? "none");
    }
}
