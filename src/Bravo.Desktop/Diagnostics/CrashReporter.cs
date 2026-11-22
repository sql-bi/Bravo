using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Bravo.Core.Browser;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// The application started to show the report of a crash of another of its processes. It takes no instance,
/// opens no log, reads no policy and builds no host: it shows the report, and ends.
/// </summary>
internal static class CrashReporter
{
    /// <summary>
    /// The switch of the command line that starts the reporter, followed by the path of the report.
    /// </summary>
    public const string SwitchName = "--crash-report";

    /// <summary>
    /// Whether the command line starts the reporter, and with which report.
    /// </summary>
    public static bool IsRequested(string[] args, [NotNullWhen(true)] out string? reportPath)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args is [SwitchName, var path])
        {
            reportPath = path;
            return true;
        }

        reportPath = null;
        return false;
    }

    /// <summary>
    /// Shows the report until the user closes it.
    /// </summary>
    /// <returns>The exit code of the process.</returns>
    public static int Run(string reportPath, IBrowserLauncher browserLauncher)
    {
        CrashReport report;
        try
        {
            report = CrashReportFile.Read(reportPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException
            or ArgumentException or NotSupportedException)
        {
            CrashReportDialog.ShowUnreadable(reportPath);
            return ExitCode.Success;
        }

        CrashReportDialog.Show(report, reportPath, browserLauncher);
        return ExitCode.Success;
    }
}
