using System;
using System.IO;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Runs Bravo as the reporter of a crash: <c>--crash-report &lt;file&gt;</c>, started by the process that crashed.
/// </summary>
/// <remarks>
/// The reporter never claims the application instance and never reports its own crashes: a crash of the reporter
/// would otherwise start another reporter. The runtime still writes its Event Log entry.
/// </remarks>
internal static class BravoCrashReporter
{
    /// <summary>
    /// The folder of the crash reports, computed without AppEnvironment, whose initialization can be the crash.
    /// </summary>
    public static string DirectoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify),
        "Bravo",
        "crashes");

    /// <returns>
    /// <see langword="true"/> when the arguments asked for the reporter, which has run and the process must exit.
    /// </returns>
    public static bool RunIfRequested(string[] args)
    {
        if (args is not [CrashReporterLauncher.ReportOption, var reportPath, ..])
        {
            return false;
        }

        GlobalExceptionHandling.Configure(static (_, _) => { });

#if DEBUG
        CrashTest.RunIfRequested(args, out _);
#endif

        var store = new CrashReportStore(DirectoryPath);
        var reporter = new CrashReporter(store, new TelemetryServiceFactory(), new CrashReportTaskDialog());

        reporter.Run(reportPath);
        return true;
    }
}
