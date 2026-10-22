using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sqlbi.Bravo.Infrastructure.Windows.Dialogs;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;

/// <summary>
/// Shows the report of a crash in a task dialog, on the thread that calls it, which must be STA.
/// </summary>
/// <remarks>
/// The stack trace is not shown: a task dialog neither scrolls nor uses a fixed-width font. Copy details and the
/// report file carry it.
/// </remarks>
internal sealed class CrashReportTaskDialog : ICrashReportDialog
{
    private const string SendStatus = "Send the report to help us fix the problem.";
    private const string SendingStatus = "Sending the report...";
    private const string SentStatus = "The report has been sent. Thank you.";
    private const string NotSentStatus = "The report could not be sent.";
    private const string NotAllowedStatus = "The report cannot be sent because telemetry is turned off.";
    private const string UnreadableStatus = "The report cannot be sent because it could not be read.";

    public void Show(CrashReportPresentation presentation)
    {
        // The continuations of a send come back to this thread, where the dialog runs its message loop.
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

        var report = presentation.Report;
        var sendButton = new TaskDialogButton("&Send report") { AllowCloseDialog = false, Enabled = presentation.Send is not null };
        var copyButton = new TaskDialogButton("&Copy details") { AllowCloseDialog = false, Visible = report is not null };
        var openFolderButton = new TaskDialogButton("&Open folder") { AllowCloseDialog = false };

        var builder = TaskDialogBuilder.Create()
            .WithCaption(AppEnvironment.ApplicationMainWindowTitle)
            .WithIcon(TaskDialogIcon.ShieldErrorRedBar)
            .WithAllowCancel()
            .WithSizeToContent()
            .WithHeading("Bravo closed because of an unexpected error")
            .WithText(GetText(report))
            .WithFootnote(GetInitialStatus(presentation), TaskDialogIcon.Information)
            .AddButtons(sendButton, copyButton, openFolderButton, TaskDialogButton.Close)
            .WithDefaultButton(TaskDialogButton.Close);

        var page = builder.Build();
        openFolderButton.Click += (_, _) => OpenFolder(page, presentation.ReportPath);

        if (report is not null)
        {
            copyButton.Click += (_, _) => CopyDetails(page, report);
        }

        if (presentation.Send is { } send)
        {
            sendButton.Click += async (_, _) => await SendAsync(page, sendButton, send);
        }

        builder.Show();
    }

    private static string GetText(CrashReport? report)
    {
        if (report is null)
        {
            return "The details of the error could not be read from the report.";
        }

        var time = report.Timestamp.ToLocalTime().ToString("G", CultureInfo.CurrentCulture);

        return $"""
            Version: {report.Version}
            Time: {time}
            Session: {report.SessionId ?? "not available"}

            {GetTypeName(report.ExceptionType)}: {report.ExceptionMessage}
            """;
    }

    // The name without its namespace, as in "InvalidOperationException": the details the user copies keep the full name.
    private static string GetTypeName(string fullName) => fullName[(fullName.LastIndexOf('.') + 1)..];

    private static string GetInitialStatus(CrashReportPresentation presentation)
    {
        if (presentation.Report is null)
        {
            return UnreadableStatus;
        }

        return presentation.Send is null ? NotAllowedStatus : SendStatus;
    }

    // Sent once: the button stays disabled after a send, and is enabled again only to retry one that failed.
    private static async Task SendAsync(TaskDialogPage page, TaskDialogButton sendButton, Func<Task<bool>> send)
    {
        sendButton.Enabled = false;
        SetStatus(page, SendingStatus);

        string status;
        try
        {
            status = await send() ? SentStatus : NotSentStatus;
        }
        catch (Exception ex)
        {
            status = GetFailureStatus(NotSentStatus, ex);
        }

        sendButton.Enabled = status != SentStatus;
        SetStatus(page, status);
    }

    private static void CopyDetails(TaskDialogPage page, CrashReport report)
    {
        var text = $"""
            Bravo {report.Version}, session {report.SessionId ?? "not available"}, {report.Timestamp:u}

            {report.ExceptionText}
            """;

        try
        {
            Clipboard.SetDataObject(new DataObject(DataFormats.UnicodeText, text), copy: true, retryTimes: 10, retryDelay: 100);
            SetStatus(page, "The details have been copied to the clipboard.");
        }
        catch (Exception ex)
        {
            SetStatus(page, GetFailureStatus("The details could not be copied to the clipboard.", ex));
        }
    }

    private static void OpenFolder(TaskDialogPage page, string reportPath)
    {
        // With its full path, as ProcessHelper starts it, and with the report selected, so that the user can attach it.
        var explorerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
        try
        {
            using var _ = Process.Start(explorerPath, $"/select,\"{reportPath}\"");
        }
        catch (Exception ex)
        {
            SetStatus(page, GetFailureStatus("The folder of the report could not be opened.", ex));
        }
    }

    // Every button reports its failure with the exception, so that a problem the user reports can be diagnosed. All
    // exceptions are caught: one that escaped a button would end the reporter, which reports no crash of its own.
    private static string GetFailureStatus(string status, Exception exception)
    {
        return $"{status} {exception.GetType().Name}: {exception.Message}";
    }

    private static void SetStatus(TaskDialogPage page, string status)
    {
        if (page.BoundDialog is null || page.Footnote is null)
        {
            return;
        }

        page.Footnote.Text = status;
    }
}
