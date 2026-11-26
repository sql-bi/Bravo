using System;
using System.Diagnostics;
using Bravo.Core.Policies;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Hosting;
using Bravo.Desktop.Policies;
using Bravo.Desktop.SingleInstance;
using Bravo.Desktop.Telemetry;
using Bravo.Platform.Windows.Browser;
using Bravo.Platform.Windows.Policies;
using Bravo.Platform.Windows.SingleInstance;

namespace Bravo.Desktop;

internal static partial class Program
{
    // Synchronous: an asynchronous entry point does not run on the STA thread that Windows Forms needs.
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Until the data folder is known, a crash is a message of this process.
        var crashReporting = new CrashReporting(
            TimeProvider.System,
            new CrashReporterProcess(),
            FatalExceptionDialog.Show,
            PersonalNames.OfCurrentUser);
        LastChanceExceptionHandling.Install(crashReporting);

        // Read once: a development build is another instance than the installed application.
        var identity = BuildIdentity.FromAssembly(typeof(Program).Assembly);

        // Started by a process that crashed, to show its report: nothing else of the application runs.
        if (CrashReporter.IsRequested(args, identity, out var reporterStart))
        {
            return CrashReporter.Run(reporterStart, identity, new DefaultBrowserLauncher());
        }

        var start = ActivationCommandLine.Parse(args, identity);
        var dataFolder = DataFolder.For(identity, start.Development);

        // The run, as its telemetry names it: what the host and the page send, and the report of a crash, share it.
        var session = TelemetrySession.Start();
        crashReporting.WriteReportsIn(dataFolder, identity.Version, session, start.Development);

        // Before the application reads or writes anything of its own: a second process ends in the arbitration.
        var names = InstanceNames.ForCurrentUserSession(identity.ApplicationId);
        var options = new SingleInstanceOptions();
        var arbiter = new SingleInstanceArbiter(names, options, TimeProvider.System);
        var arbitration = arbiter.Arbitrate(ActivationPayload.Encode(start.Request));
        if (!arbitration.IsOwner)
        {
            return SecondInstance.End(arbitration);
        }

        // Released last, after the host is disposed: until then no other process becomes the owner.
        using (arbitration.Ownership)
        {
            // The records of the start share a trace, the one of a log file that cannot be created included. It
            // ends before the host starts, whose requests and services have traces of their own.
            using var startActivity = new Activity("Bravo.Start").Start();

            // Closed after the host: what the host writes while it stops is recorded.
            using var log = HostLog.Open(dataFolder, writeToEventLog: !identity.IsDevelopmentBuild, TimeProvider.System);
            using var crashLog = crashReporting.AttachLog(log);
            using var unobservedTaskExceptionLog = UnobservedTaskExceptionLog.Attach(log.Factory);
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
                var failureExitCode = PolicyReadFailureDialog.End(exception);
                LogEnded(logger, failureExitCode);

                return failureExitCode;
            }

            using var host = DesktopHost.Build(
                identity, dataFolder, start.Development, policies, arbitration.Ownership, options, log, session);
            startActivity.Stop();

            var exitCode = DesktopHost.Run(host, start.Request);
            LogEnded(logger, exitCode);

            return exitCode;
        }
    }
}
