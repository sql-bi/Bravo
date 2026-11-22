using System.Globalization;
using System.Text;
using Bravo.Api.Diagnostics;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// How a record is written in the file of the log: on one line, beginning with its time, and the lines of a
/// message on more lines and of an exception below it, indented. A line that is not indented begins a record.
/// </summary>
internal static class LogFileLine
{
    private const string NewLine = "\r\n";
    private const string Indent = "    ";

    // Without a trace, the place of the trace still has a word: every line splits into the same fields.
    private const string NoTrace = "-";

    public static string Format(LogRecord record)
    {
        var line = new StringBuilder();
        line.Append(CultureInfo.InvariantCulture, $"{record.Time.UtcDateTime:yyyy-MM-dd'T'HH:mm:ss.fff'Z'} ");
        line.Append(CultureInfo.InvariantCulture, $"{record.Level} {record.TraceId ?? NoTrace} ");
        line.Append(CultureInfo.InvariantCulture, $"{record.Category}[{record.EventId}] ");
        line.Append(Indented(record.Message));

        if (record.Exception is not null)
        {
            line.Append(NewLine).Append(Indent).Append(Indented(record.Exception));
        }

        return line.ToString();
    }

    private static string Indented(string text)
    {
        return text.ReplaceLineEndings(NewLine + Indent);
    }
}
