using System.Text.RegularExpressions;

namespace Sqlbi.Bravo.Infrastructure.Telemetry;

/// <summary>
/// Redacts the text that Analysis Services and other libraries mark as sensitive in error messages, with the tags
/// <![CDATA[<pii>, <oii>, <euii>, <eupi>, <conn>]]>.
/// </summary>
/// <remarks>
/// The same tags are redacted by <c>Utils.Obj.anonymize</c> in <c>src/Scripts/helpers/utils.ts</c> when the diagnostics
/// pane copies a message; a test checks that both lists match.
/// </remarks>
internal static partial class SensitiveTags
{
    internal const string TagNames = "pii|oii|euii|eupi|conn";

    private const string RedactedText = "[REDACTED]";

    public static string Redact(string? input)
    {
        if (input is null)
        {
            return string.Empty;
        }

        try
        {
            return TagRegex().Replace(input, $"<$1>{RedactedText}</$1>");
        }
        catch (RegexMatchTimeoutException)
        {
            // A text too costly to scan, such as one with many unclosed tags, is withheld rather than let through.
            return RedactedText;
        }
    }

    [GeneratedRegex(@"<(" + TagNames + @")>(.*?)</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TagRegex();
}
