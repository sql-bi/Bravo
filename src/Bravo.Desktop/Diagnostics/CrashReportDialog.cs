using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Bravo.Core.Browser;

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

    /// <summary>
    /// A new issue of the repository, with no template and no field named: a change of the templates cannot
    /// break it.
    /// </summary>
    public static readonly Uri NewIssueAddress = new("https://github.com/sql-bi/Bravo/issues/new");

    private const char Ellipsis = '…';

    public static void Show(CrashReport report, string reportPath, IBrowserLauncher browserLauncher)
    {
        var reportName = Path.GetFileName(reportPath);
        var footnote = new TaskDialogFootnote(
            "Nothing has been sent. The report holds no name of the user or of the computer.");

        // The dialog stays open after each of these: the user can report and then copy, or open the folder.
        var reportIssue = new TaskDialogButton("&Report on GitHub") { AllowCloseDialog = false };
        var copyDetails = new TaskDialogButton("&Copy details") { AllowCloseDialog = false };
        var openFolder = new TaskDialogButton("&Open folder") { AllowCloseDialog = false };
        var close = TaskDialogButton.Close;

        reportIssue.Click += (_, _) => Try(() => browserLauncher.Open(NewIssueAddress));
        var copiedText = CreateCopiedText(report, reportName);
        copyDetails.Click += (_, _) => footnote.Text = Try(() => Clipboard.SetText(copiedText))
            ? "The details are in the clipboard."
            : "The details could not be copied: try again.";
        openFolder.Click += (_, _) => Try(() => ShowInFolder(reportPath));

        ShowPage(new TaskDialogPage
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
        });
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
