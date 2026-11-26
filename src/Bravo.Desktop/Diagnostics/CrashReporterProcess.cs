using System;
using System.ComponentModel;
using System.Diagnostics;
using Bravo.Desktop.Development;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// Starts the executable of this process as the reporter.
/// </summary>
internal sealed class CrashReporterProcess : ICrashReporterLauncher
{
    public bool TryStart(string reportPath, DevelopmentOptions development)
    {
        if (Environment.ProcessPath is not { } executable)
        {
            return false;
        }

        var startInfo = new ProcessStartInfo(executable, CrashReporter.CreateArguments(reportPath, development))
        {
            UseShellExecute = false,
        };

        try
        {
            using var process = Process.Start(startInfo);
            return process is not null;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
