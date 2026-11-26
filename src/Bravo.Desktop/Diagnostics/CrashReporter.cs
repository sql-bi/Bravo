using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using Bravo.Core.Browser;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Development;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// The application started to show the report of a crash of another of its processes, and to send it with the
/// telemetry. It takes no instance, opens no log and builds no host: it shows the report, and ends.
/// </summary>
internal static class CrashReporter
{
    /// <summary>
    /// The switch of the command line that starts the reporter, followed by the path of the report.
    /// </summary>
    public const string SwitchName = "--crash-report";

    // How long the reporter waits, once the user closed its dialog, for a send that has not ended.
    private static readonly TimeSpan s_sendingWait = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Whether the command line starts the reporter, with which report and, in a development build, with the
    /// options of the process that crashed.
    /// </summary>
    public static bool IsRequested(
        string[] args,
        BuildIdentity identity,
        [NotNullWhen(true)] out CrashReporterStart? start)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(identity);

        start = null;
        if (args is not [SwitchName, var reportPath, .. var options])
        {
            return false;
        }

        // The external tool of Power BI Desktop starts the application with arguments of its own: only options of
        // a development build, each with its value, follow the report.
        if (options.Length > 0 && (!identity.IsDevelopmentBuild || !options.All(IsDevelopmentOption)))
        {
            return false;
        }

        var development = options.Length == 0
            ? DevelopmentOptions.None
            : ActivationCommandLine.Parse(options, identity).Development;
        start = new CrashReporterStart(reportPath, development);
        return true;
    }

    /// <summary>
    /// The command line of the reporter of a report: in a development build, with the options of the process
    /// that crashed that the reporter needs to send it, its data folder, its policies and its endpoint.
    /// </summary>
    public static List<string> CreateArguments(string reportPath, DevelopmentOptions development)
    {
        ArgumentNullException.ThrowIfNull(development);

        List<string> arguments = [SwitchName, reportPath];
        AddOption(arguments, ActivationCommandLine.DataFolderOptionName, development.DataFolderPath);
        AddOption(arguments, ActivationCommandLine.PolicyKeyOptionName, development.PolicyKeyName);
        AddOption(arguments, ActivationCommandLine.TelemetryEndpointOptionName, development.TelemetryEndpoint);
        return arguments;
    }

    /// <summary>
    /// Shows the report until the user closes it, and sends it as the telemetry allows.
    /// </summary>
    /// <returns>The exit code of the process.</returns>
    public static int Run(CrashReporterStart start, BuildIdentity identity, IBrowserLauncher browserLauncher)
    {
        ArgumentNullException.ThrowIfNull(start);

        CrashReport report;
        try
        {
            report = CrashReportFile.Read(start.ReportPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException
            or ArgumentException or NotSupportedException)
        {
            CrashReportDialog.ShowUnreadable(start.ReportPath);
            return ExitCode.Success;
        }

        var submission = CrashReportSubmission.For(report, start, identity);
        CrashReportDialog.Show(report, start.ReportPath, browserLauncher, submission);
        submission.WaitForSending(s_sendingWait);

        return ExitCode.Success;
    }

    private static bool IsDevelopmentOption(string argument)
    {
        return argument.StartsWith("--dev-", StringComparison.Ordinal) && argument.Contains('=', StringComparison.Ordinal);
    }

    private static void AddOption(List<string> arguments, string name, string? value)
    {
        if (value is not null)
        {
            arguments.Add($"{name}={value}");
        }
    }
}
