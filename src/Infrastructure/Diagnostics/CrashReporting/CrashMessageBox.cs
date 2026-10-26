using System.Windows.Forms;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;

/// <summary>
/// Tells the user that Bravo crashed when the crash dialog is not available: a message box needs no type that the
/// crash can have broken.
/// </summary>
internal static class CrashMessageBox
{
    public const string Heading = "Bravo closed because of an unexpected error";

    public static void Show(string details)
    {
        var text = $"""
            {Heading}.

            {details}
            """;

        MessageBox.Show(text, AppEnvironment.ApplicationMainWindowTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
