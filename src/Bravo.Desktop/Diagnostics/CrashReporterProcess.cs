using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// Starts the executable of this process as the reporter.
/// </summary>
internal sealed class CrashReporterProcess : ICrashReporterLauncher
{
    public bool TryStart(string reportPath)
    {
        if (Environment.ProcessPath is not { } executable)
        {
            return false;
        }

        var startInfo = new ProcessStartInfo(executable)
        {
            ArgumentList = { CrashReporter.SwitchName, reportPath },
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
