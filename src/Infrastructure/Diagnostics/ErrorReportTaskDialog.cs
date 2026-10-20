using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Sqlbi.Bravo.Infrastructure.Windows.Dialogs;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

internal interface IErrorReportDialog
{
    void Show(ErrorReport report, ITelemetryService? telemetry);
}

/// <summary>
/// Implements an error report dialog to display error information to the user.
/// </summary>
internal sealed class ErrorReportTaskDialog : IErrorReportDialog
{
    public void Show(ErrorReport report, ITelemetryService? telemetry)
    {
        // Always run on a dedicated STA thread: TaskDialog and Clipboard require STA,
        // while the caller could be executing on any arbitrary background thread.
        ProcessHelper.RunOnSTAThread(() =>
        {
            try
            {
                ShowImpl(report, telemetry);
            }
            catch
            {
                // The task dialog uses application types that can be the cause of the crash.
                ShowFallback(report);
                throw;
            }
        });
    }

    private static void ShowFallback(ErrorReport report)
    {
        try
        {
            var text = new StringBuilder();
            text.AppendLine("Bravo encountered an unexpected error and will close.");

            if (report.FilePath is not null)
            {
                text.AppendLine();
                text.AppendLine("A crash report has been saved:");
                text.AppendLine(report.FilePath);
            }

            text.AppendLine();
            text.Append(report.Exception.GetType().Name).Append(": ").Append(report.Exception.Message);

            MessageBox.Show(text.ToString(), "Bravo for Power BI", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch { /* Ignore failures */ }
    }

    private static void ShowImpl(ErrorReport report, ITelemetryService? telemetry)
    {
        // Ensure a WindowsFormsSynchronizationContext exists on this STA thread so that async continuations
        // post back to this thread's message loop, where the modal dialog can safely process UI updates.
        if (SynchronizationContext.Current is not WindowsFormsSynchronizationContext)
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

        // When telemetry is enabled, reports are automatically submitted; offer Send only when it is disabled.
        var sendTelemetry = telemetry is { TelemetryEnabled: false } ? telemetry : null;
        var sendButton = new TaskDialogCommandLinkButton("&Send report", "Sends the crash report to help diagnose the problem.")
        {
            AllowCloseDialog = false,
            Visible = sendTelemetry is not null,
        };
        var copyButton = new TaskDialogCommandLinkButton("&Copy to clipboard", "Copies the crash report details to the clipboard.")
        {
            AllowCloseDialog = false,
        };

        var text = GetText(report);
        var builder = TaskDialogBuilder.Create()
            .WithCaption(AppEnvironment.ApplicationMainWindowTitle)
            .WithIcon(TaskDialogIcon.ShieldErrorRedBar)
            .WithCurrentProcessMainWindowOwner()
            .WithStartupLocation(TaskDialogStartupLocation.CenterOwner)
            .WithAllowCancel()
            .WithSizeToContent()
            .WithEnableLinks((page, linkHref) => OpenFile(page, linkHref, text))
            .WithHeading("Bravo encountered an unexpected error.")
            .WithText(text)
            .AddButtons(sendButton, copyButton, TaskDialogButton.Close)
            .WithDefaultButton(sendButton.Visible ? sendButton : copyButton);

        var page = builder.Build();

        copyButton.Click += (_, _) => CopyToClipboard(page, text, report);
        if (sendTelemetry is not null)
            sendButton.Click += async (_, _) => await SendAsync(sendTelemetry, page, sendButton, text, report);

        builder.Show();
    }

    private static async Task SendAsync(ITelemetryService telemetry, TaskDialogPage page, TaskDialogButton sendButton, string text, ErrorReport report)
    {
        var previousTelemetryEnabled = telemetry.TelemetryEnabled;
        try
        {
            sendButton.Enabled = false;
            SetStatus(page, text, "Sending the report...");

            telemetry.TelemetryEnabled = true;
            telemetry.TrackException(report.Exception, properties: new Dictionary<string, string>
            {
                ["ExceptionKind"] = "Unhandled"
            });

            using var timeout = new CancellationTokenSource(30_000);
            var submitted = await telemetry.FlushAsync(timeout.Token);

            SetStatus(page, text, submitted
                ? "The report has been submitted. Thank you."
                : "The report could not be submitted. Copy the report to keep its details.");
        }
        catch (Exception ex)
        {
            SetStatus(page, text, "The report could not be submitted. Copy the report to keep its details.", ex);
        }
        finally
        {
            telemetry.TelemetryEnabled = previousTelemetryEnabled;
        }
    }

    private static void CopyToClipboard(TaskDialogPage page, string text, ErrorReport report)
    {
        try
        {
            Clipboard.SetDataObject(
                data: new DataObject(DataFormats.UnicodeText, report.Text),
                copy: true,
                retryTimes: 10,
                retryDelay: 100);
        }
        catch (Exception ex)
        {
            SetStatus(page, text, "The report could not be copied to the clipboard.", ex);
        }
    }

    private static void OpenFile(TaskDialogPage page, string linkHref, string text)
    {
        try
        {
            // The href must be a file path and the file must exist.
            if (!File.Exists(linkHref))
            {
                SetStatus(page, text, "The report file is no longer available.");
                return;
            }

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = linkHref,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            SetStatus(page, text, "The report file could not be opened.", ex);
        }
    }

    private static void SetStatus(TaskDialogPage page, string text, string status, Exception? exception = null)
    {
        if (page.BoundDialog is null)
            return;

        if (exception is not null)
            status = $"{status} ({exception.GetType().Name}: {exception.Message})";

        page.Text = $"""
            {text}
            ----

            {status}
            """;
    }

    private static string GetText(ErrorReport report)
    {
        var text = new StringBuilder();

        text.AppendLine(report.FilePath is not null
            ? "A crash report has been saved and can be used to diagnose the problem."
            : "The crash report could not be saved locally.");

        text.AppendLine();
        text.AppendLine("SessionId:");
        text.AppendLine(TelemetrySessionInfo.SessionId);

        if (report.FilePath is not null)
        {
            text.AppendLine();
            text.AppendLine("Report file:");
            text.Append("<a href=\"").Append(report.FilePath).Append("\">").Append(report.FilePath).AppendLine("</a>");
        }

        text.AppendLine();
        text.AppendLine("Exception:");
        text.AppendLine($"{report.Exception.GetType().Name}: {report.Exception.Message}");

        //text.AppendLine();
        //text.AppendLine("Location:");
        //text.AppendLine(GetExceptionLocation(report.Exception));

        return text.ToString();

        //static string GetExceptionLocation(Exception exception)
        //{
        //    if (exception.TargetSite is { } method && method.DeclaringType is { } type)
        //    {
        //        return $"{type.FullName}.{method.Name}";
        //    }

        //    return exception.Source ?? "<none>";
        //}
    }
}
