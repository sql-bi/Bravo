using System;
using System.Windows.Forms;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// Tells the user, from the process that crashes, that the application is ending because of an error: when no
/// report could be written, or the reporter could not start.
/// </summary>
internal static class FatalExceptionDialog
{
    /// <param name="exception">The exception that nothing caught.</param>
    /// <param name="reportPath">The report of the crash, or null when none was written.</param>
    public static void Show(Exception exception, string? reportPath)
    {
        _ = MessageBox.Show(
            CreateText(exception, reportPath), Product.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    internal static string CreateText(Exception exception, string? reportPath)
    {
        var separator = Environment.NewLine + Environment.NewLine;
        var text = $"{Product.Name} met an unexpected error and has to close.{separator}"
            + $"{exception.GetType().FullName}: {exception.Message}";
        return reportPath is null ? text : $"{text}{separator}The details are in {reportPath}.";
    }
}
