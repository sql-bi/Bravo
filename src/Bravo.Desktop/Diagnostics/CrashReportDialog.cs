using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bravo.Core.Browser;
using Bravo.Desktop.Telemetry;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// Tells the user that the application closed because of an error, with what support asks first in view, for
/// a screenshot, and offers the form of a bug report, the details to paste and the folder of the report.
/// </summary>
internal static class CrashReportDialog
{
    /// <summary>
    /// How many lines of the text of the exception the details show: a dialog does not scroll, and Copy details
    /// has them all.
    /// </summary>
    public const int MaxDetailLines = 20;

    /// <summary>
    /// How many lines of the message the text shows. A message can hold a whole query.
    /// </summary>
    public const int MaxMessageLines = 3;

    /// <summary>
    /// The longest line that the dialog shows whole: a longer one is cut, and Copy details has it whole.
    /// </summary>
    public const int MaxLineLength = 300;

    private const string IssueFormAddress =
        "https://github.com/sql-bi/Bravo/issues/new?labels=bug%2Cuntriaged&template=bug_report.yml";

    private const char Ellipsis = '…';

    private const string SendingStatus = "Sending with the telemetry…";

    public static void Show(
        CrashReport report,
        string reportPath,
        IBrowserLauncher browserLauncher,
        CrashReportSubmission submission)
    {
        var reportName = Path.GetFileName(reportPath);

        // What became of the telemetry of the crash, then whether Copy details worked.
        var status = DescribeMode(submission.Mode);
        string? copied = null;
        var footnote = new TaskDialogFootnote(CreateFootnote(status, copied));

        void UpdateFootnote()
        {
            footnote.Text = CreateFootnote(status, copied);
        }

        void ShowStatus(string text)
        {
            status = text;
            UpdateFootnote();
        }

        // The dialog stays open after each of these: the user can report and then copy, or open the folder.
        var send = new TaskDialogButton("&Send") { AllowCloseDialog = false };
        var reportIssue = new TaskDialogButton("&Report on GitHub") { AllowCloseDialog = false };
        var copyDetails = new TaskDialogButton("&Copy details") { AllowCloseDialog = false };
        var openFolder = new TaskDialogButton("&Open folder") { AllowCloseDialog = false };
        var close = TaskDialogButton.Close;

        var issueAddress = CreateIssueAddress(report);
        reportIssue.Click += (_, _) => Try(() => browserLauncher.Open(issueAddress));
        var copiedText = CreateCopiedText(report, reportName);
        copyDetails.Click += (_, _) =>
        {
            copied = Try(() => Clipboard.SetText(copiedText))
                ? "The details are in the clipboard."
                : "The details could not be copied: try again.";
            UpdateFootnote();
        };
        openFolder.Click += (_, _) => Try(() => ShowInFolder(reportPath));

        Task SendCrashAsync()
        {
            return SendAsync(send, submission.SendAsync, ShowStatus);
        }

        send.Click += async (_, _) => await SendCrashAsync().ConfigureAwait(true);

        var page = new TaskDialogPage
        {
            Text = CreateText(report),
            Expander = new TaskDialogExpander(CreateDetails(report, reportName))
            {
                CollapsedButtonText = "Show details",
                ExpandedButtonText = "Hide details",
                Position = TaskDialogExpanderPosition.AfterText,
            },
            Footnote = footnote,
            Buttons = { reportIssue, copyDetails, openFolder, close },
            DefaultButton = close,
        };
        if (submission.Mode == CrashReportSubmissionMode.OnRequest)
        {
            page.Buttons.Insert(0, send);
        }
        else if (submission.Mode == CrashReportSubmissionMode.Automatic)
        {
            page.Created += async (_, _) => await SendCrashAsync().ConfigureAwait(true);
        }

        // The dialog runs its own message loop, which the context posts to. Never disposed: a send that ends while
        // the reporter ends would post to a context being disposed.
        if (SynchronizationContext.Current is null)
        {
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        }

        ShowPage(page);
    }

    public static void ShowUnreadable(string reportPath)
    {
        ShowPage(new TaskDialogPage
        {
            Text = $"Its report cannot be read: {reportPath}",
            Buttons = { TaskDialogButton.Close },
        });
    }

    /// <summary>
    /// What support asks first, in view for a screenshot: the message and the short type of the exception, the
    /// version, the time and the trace of the record of the crash.
    /// </summary>
    internal static string CreateText(CrashReport report)
    {
        // The name of a generic type ends with its arguments, whose names and versions have dots as well.
        var type = report.Exception.Type.Split('[')[0];
        var shortType = type[(type.LastIndexOf('.') + 1)..];
        string[] lines =
        [
            $"{Shorten(report.Exception.Message)} ({shortType})",
            string.Empty,
            $"Version: {report.Version}",
            FormatTime(report),
            $"Trace ID: {report.Trace ?? "none"}",
        ];
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Sends the crash and says what became of it. Send stays off, whatever the end: the submission sends once.
    /// The send never throws, and its end comes back to the thread of the dialog.
    /// </summary>
    internal static async Task SendAsync(
        TaskDialogButton send,
        Func<Task<TelemetryDelivery>> sendCrash,
        Action<string> showStatus)
    {
        send.Enabled = false;
        showStatus(SendingStatus);
        var delivery = await sendCrash().ConfigureAwait(true);
        showStatus(DescribeDelivery(delivery));
    }

    /// <summary>
    /// What the footnote says of the telemetry of the crash before any send.
    /// </summary>
    internal static string DescribeMode(CrashReportSubmissionMode mode)
    {
        return mode switch
        {
            CrashReportSubmissionMode.NoTelemetry => "Not sent: this build has no telemetry.",
            CrashReportSubmissionMode.EarlierFormat => $"Not sent: an earlier version of {Product.ShortName} wrote this report.",
            CrashReportSubmissionMode.PoliciesUnreadable => $"Not sent: the policies of {Product.ShortName} cannot be read.",
            CrashReportSubmissionMode.Forbidden => "Not sent: a policy turns telemetry off.",
            CrashReportSubmissionMode.Automatic => SendingStatus,
            CrashReportSubmissionMode.OnRequest => "Not sent: telemetry is off. Send sends this error alone.",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "No text for the mode."),
        };
    }

    /// <summary>
    /// What the footnote says once a send ended.
    /// </summary>
    internal static string DescribeDelivery(TelemetryDelivery delivery)
    {
        return delivery switch
        {
            TelemetryDelivery.Sent => "Sent with the telemetry.",
            TelemetryDelivery.Kept => $"Not sent yet: {Product.ShortName} sends it at its next start.",
            TelemetryDelivery.NotSent => "Sending failed.",
            _ => throw new ArgumentOutOfRangeException(nameof(delivery), delivery, "No text for the delivery."),
        };
    }

    /// <summary>
    /// The footnote: what became of the telemetry of the crash, then whether Copy details worked, or what the
    /// report leaves out.
    /// </summary>
    internal static string CreateFootnote(string status, string? copied)
    {
        return status + Environment.NewLine + (copied ?? "The report holds no name of the user or of the computer.");
    }

    internal static string CreateDetails(CrashReport report, string reportName)
    {
        var text = report.Exception.Text;
        var lines = text.Take(MaxDetailLines).Select(ShortenLine).ToList();
        if (text.Count > MaxDetailLines)
        {
            lines.Add($"{text.Count - MaxDetailLines} more lines: Copy details has them all.");
        }

        if (lines.Count > 0)
        {
            lines.Add(string.Empty);
        }

        lines.Add($"Log file: {report.LogFile ?? "none"}");
        lines.Add($"Report: {reportName}");
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// What Copy details gives the user to paste: the facts of the dialog, then the whole text of the exception.
    /// </summary>
    internal static string CreateCopiedText(CrashReport report, string reportName)
    {
        string[] lines =
        [
            $"{Product.Name} {report.Version}",
            FormatTime(report),
            $"Trace ID: {report.Trace ?? "none"}",
            $"Log file: {report.LogFile ?? "none"}",
            $"Report: {reportName}",
            string.Empty,
            .. report.Exception.Text,
        ];
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// The form of a bug report that the page opens too, with the version and the trace that the dialog shows. The
    /// message and the stack stay out of the address: the user pastes or attaches them.
    /// </summary>
    internal static Uri CreateIssueAddress(CrashReport report)
    {
        var address = $"{IssueFormAddress}&application-version={Uri.EscapeDataString(report.Version)}";
        if (report.Trace is not null)
        {
            address += $"&logs={Uri.EscapeDataString($"Trace ID: {report.Trace}")}";
        }

        return new Uri(address);
    }

    private static string FormatTime(CrashReport report)
    {
        return string.Create(CultureInfo.InvariantCulture, $"Time: {report.Time.UtcDateTime:yyyy-MM-dd HH:mm:ss} UTC");
    }

    // The first lines of the message, with an ellipsis where it goes on.
    private static string Shorten(string message)
    {
        var lines = message.ReplaceLineEndings("\n").Split('\n');
        var shown = string.Join(Environment.NewLine, lines.Take(MaxMessageLines).Select(ShortenLine));
        return lines.Length > MaxMessageLines && !shown.EndsWith(Ellipsis) ? shown + Ellipsis : shown;
    }

    private static string ShortenLine(string line)
    {
        if (line.Length <= MaxLineLength)
        {
            return line;
        }

        // Not between the two halves of a character.
        var length = char.IsHighSurrogate(line[MaxLineLength - 1]) ? MaxLineLength - 1 : MaxLineLength;
        return line[..length] + Ellipsis;
    }

    // The same icon and heading whether the report can be read or not. Wide enough for a whole path, which
    // Windows shortens otherwise.
    private static void ShowPage(TaskDialogPage page)
    {
        using var icon = CreateIcon();
        page.Caption = Product.Name;
        page.Heading = $"{Product.ShortName} encountered an error and closed unexpectedly";
        page.Icon = icon;
        page.AllowCancel = true;
        page.SizeToContent = true;
        _ = TaskDialog.ShowDialog(page);
    }

    // The icon of the application with the overlay of an error, which Windows allows in place of the icon of an
    // error when the subject of the message has an icon of its own.
    private static TaskDialogIcon CreateIcon()
    {
        var size = SystemInformation.IconSize;
        var overlaySize = new Size(size.Width / 2, size.Height / 2);
        using var product = ProductIcon.Load();
        using var overlay = SystemIcons.GetStockIcon(StockIconId.Error, overlaySize.Width);
        using var bitmap = new Bitmap(size.Width, size.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.DrawIcon(product, new Rectangle(Point.Empty, size));
            graphics.DrawIcon(overlay, new Rectangle(new Point(overlaySize), overlaySize));
        }

        return new TaskDialogIcon(bitmap);
    }

    // Explorer, with the report selected.
    private static void ShowInFolder(string reportPath)
    {
        var explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
        Process.Start(explorer, $"/select,\"{reportPath}\"")?.Dispose();
    }

    // The clipboard can be held by another program, the browser or Explorer can fail to start: the user can try
    // again, and the dialog stays.
    private static bool Try(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }
}
