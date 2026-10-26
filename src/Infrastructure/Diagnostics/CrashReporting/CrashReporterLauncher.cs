using System.Diagnostics;
using System.IO;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;

internal interface ICrashReporterLauncher
{
    /// <summary>
    /// Starts the reporter process for a saved report, without waiting for it.
    /// </summary>
    void Launch(string reportPath);
}

/// <summary>
/// Starts the reporter: the same executable, run with <see cref="ReportOption"/>.
/// </summary>
internal sealed class CrashReporterLauncher(string executablePath) : ICrashReporterLauncher
{
    public const string ReportOption = "--crash-report";

    private readonly string _executablePath = executablePath;

    public void Launch(string reportPath)
    {
        var startInfo = CreateStartInfo(_executablePath, reportPath);

        using var process = Process.Start(startInfo);
    }

    // Without the shell, which can show UI and loads shell extensions into a process that is crashing. In the folder
    // of the executable, so that the reporter keeps no folder of the crashed process in use while its dialog is open.
    internal static ProcessStartInfo CreateStartInfo(string executablePath, string reportPath)
    {
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(executablePath),
        };

        startInfo.ArgumentList.Add(ReportOption);
        startInfo.ArgumentList.Add(reportPath);

        return startInfo;
    }
}
