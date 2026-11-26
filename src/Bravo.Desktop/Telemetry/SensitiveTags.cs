using System.Text.RegularExpressions;

namespace Bravo.Desktop.Telemetry;

/// <summary>
/// Redacts the text that Analysis Services and other libraries mark as sensitive in their messages, with the
/// tags <![CDATA[<pii>, <oii>, <euii>, <eupi>, <conn>]]>.
/// </summary>
/// <remarks>
/// The page removes the same tags when it copies a message, in <c>web/helpers/utils.ts</c>: a test checks that
/// both lists match.
/// </remarks>
internal static partial class SensitiveTags
{
    internal const string TagNames = "pii|oii|euii|eupi|conn";

    private const string RedactedText = "[REDACTED]";

    public static string Redact(string text)
    {
        try
        {
            return TagRegex().Replace(text, $"<$1>{RedactedText}</$1>");
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
