using System;
using System.Windows.Forms;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;

namespace Sqlbi.Bravo.Host;

internal static class BravoGlobalExceptionHandling
{
    public static UnhandledExceptionHandler Configure()
    {
        var handler = new UnhandledExceptionHandler(
            new CrashReportStore(BravoCrashReporter.DirectoryPath),
            new CrashReporterLauncher(Application.ExecutablePath),
            TimeProvider.System,
            ShowFallback);

        GlobalExceptionHandling.Configure(handler.Handle);

        return handler;
    }

    // Shown only when there is no report or no reporter to tell the user that Bravo failed.
    private static void ShowFallback(Exception exception)
    {
        CrashMessageBox.Show($"{exception.GetType().FullName}: {exception.Message}");
    }
}
