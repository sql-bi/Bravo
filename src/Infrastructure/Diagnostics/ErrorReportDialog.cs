using System;
using System.Text;
using System.Windows.Forms;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Windows.Dialogs;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Provides a dialog to report an error that occurred in the application.
/// </summary>
internal static class ErrorReportDialog
{
    //private const string BugReportUrl = "https://github.com/sql-bi/Bravo/issues/new";

    public static void Show(ErrorReport report)
    {
        //var reportButton = new TaskDialogCommandLinkButton("&Report this issue", "Copies the report to the clipboard and opens the Bravo issue tracker in your browser, where you can paste it.");
        var copyButton = new TaskDialogCommandLinkButton("&Copy to clipboard", "Copies the crash report to the clipboard.")
        {
            // Keep the dialog open after clicking the button
            AllowCloseDialog = false,
        };

        copyButton.Click += (_, _) => _ = report.TryCopyToClipboard();

        var clickedButton = TaskDialogBuilder.Create()
            .WithCaption(AppEnvironment.ApplicationMainWindowTitle)
            .WithIcon(TaskDialogIcon.Error)
            .WithStartupLocation(TaskDialogStartupLocation.CenterScreen)
            .WithAllowCancel()
            .WithSizeToContent()
            .WithEnableLinks((href) => _ = ProcessHelper.Open(href))
            .WithHeading("Bravo encountered an unexpected error.")
            .WithText(GetText(report))
            .AddButtons(/*reportButton,*/ copyButton, TaskDialogButton.Close)
            .WithDefaultButton(copyButton)
            .Show();

        //if (clickedButton == reportButton)
        //{
        //    _ = report.TryCopyToClipboard();
        //    _ = ProcessHelper.OpenBrowser(new Uri(BugReportUrl, UriKind.Absolute));
        //}
    }

    private static string GetText(ErrorReport report)
    {
        var text = new StringBuilder();

        text.AppendLine(report.FilePath is not null
            ? "A crash report has been saved and can be used to diagnose the problem."
            : "The crash report could not be saved.");

        if (report.FilePath is not null)
        {
            text.AppendLine();
            text.AppendLine("Report file:");
            text.Append("<a href=\"").Append(report.FilePath).Append("\">").Append(report.FilePath).AppendLine("</a>");
        }

        text.AppendLine();
        text.AppendLine("Exception:");
        text.AppendLine($"{report.Exception.GetType().FullName}: {report.Exception.Message}");

        text.AppendLine();
        text.AppendLine("Location:");
        text.AppendLine(GetExceptionLocation(report.Exception));

        return text.ToString();

        static string GetExceptionLocation(Exception exception)
        {
            if (exception.TargetSite is { } method)
            {
                if (method.DeclaringType is { } type)
                {
                    return $"{type.FullName}.{method.Name}";
                }
            }
             
            return exception.Source ?? "<none>";
        }
    }
}
