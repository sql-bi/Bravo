using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Bravo.Desktop.WebView;

/// <summary>
/// Tells the user why the application ends because of its webview.
/// </summary>
internal static class WebViewFailureDialog
{
    public static void Show(IWin32Window owner, WebViewFailureMessage message)
    {
        var page = new TaskDialogPage
        {
            Caption = Product.Name,
            Heading = message.Heading,
            Text = message.Text,
            Icon = TaskDialogIcon.Error,
            AllowCancel = true,
            Buttons = { TaskDialogButton.Close },
        };

        if (message.LinkAddress is { } address)
        {
            var separator = Environment.NewLine + Environment.NewLine;
            page.Text = $"{message.Text}{separator}<a href=\"{address}\">{address}</a>";
            page.EnableLinks = true;
            page.LinkClicked += (_, _) => OpenInBrowser(address);
        }

        _ = TaskDialog.ShowDialog(owner, page);
    }

    // The address is the one of the message, never text that came from outside.
    private static void OpenInBrowser(string address)
    {
        var browser = new ProcessStartInfo(address) { UseShellExecute = true };
        using var process = Process.Start(browser);
    }
}
