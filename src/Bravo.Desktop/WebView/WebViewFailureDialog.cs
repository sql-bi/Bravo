using System;
using System.Windows.Forms;
using Bravo.Core.Browser;

namespace Bravo.Desktop.WebView;

/// <summary>
/// Tells the user why the application ends because of its webview.
/// </summary>
internal static class WebViewFailureDialog
{
    public static void Show(IWin32Window owner, WebViewFailureMessage message, IBrowserLauncher browserLauncher)
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
            page.LinkClicked += (_, _) => browserLauncher.Open(new Uri(address));
        }

        _ = TaskDialog.ShowDialog(owner, page);
    }
}
