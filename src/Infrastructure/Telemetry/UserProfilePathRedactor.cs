using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Sqlbi.Bravo.Infrastructure.Telemetry;

/// <summary>
/// Replaces the folders of the user profile with environment variables, such as <c>%LOCALAPPDATA%</c>, so that text
/// that leaves the machine keeps its paths readable without the Windows user name.
/// </summary>
/// <remarks>
/// A folder matches with either separator, so file URIs match too, and in any case. It matches only as a whole path
/// segment: <c>C:\Users\al</c> does not match inside <c>C:\Users\alberto</c> or <c>C:\Users\al.old</c>, but it does
/// before punctuation that ends a sentence, as in <c>denied: C:\Users\al.</c>.
/// </remarks>
internal sealed class UserProfilePathRedactor
{
    private const string AnySeparator = @"[\\/]";
    private const string SegmentEnd = @"(?=$|[\\/""'\s;,)\]}>]|[.:!?](?:$|[\s""')\]}>]))";

    private static readonly char[] s_separators = ['\\', '/'];

    private readonly Lazy<(Regex Pattern, string Variable)[]> _replacements;

    /// <param name="folders">Each folder and the variable that replaces it; folders that are empty are ignored.</param>
    public UserProfilePathRedactor(IEnumerable<KeyValuePair<string, string>> folders)
    {
        // The longest folder first, so that %LOCALAPPDATA% wins over the %USERPROFILE% that contains it.
        KeyValuePair<string, string>[] orderedFolders = [.. folders
            .Where((folder) => !string.IsNullOrWhiteSpace(folder.Key))
            .OrderByDescending((folder) => folder.Key.TrimEnd(s_separators).Length)];

        // Built on first use: telemetry creates a redactor at every start, and most sessions never redact. A failure is
        // not cached, so that the next redaction tries again instead of failing for the rest of the session.
        _replacements = new(
            () => [.. orderedFolders.Select((folder) => (CreatePattern(folder.Key), EscapeReplacement(folder.Value)))],
            LazyThreadSafetyMode.PublicationOnly);
    }

    public static UserProfilePathRedactor CreateForCurrentUser()
    {
        return new UserProfilePathRedactor(
        [
            new(GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "%LOCALAPPDATA%"),
            new(GetFolderPath(Environment.SpecialFolder.ApplicationData), "%APPDATA%"),
            new(GetFolderPath(Environment.SpecialFolder.UserProfile), "%USERPROFILE%"),
        ]);

        static string GetFolderPath(Environment.SpecialFolder folder)
            => Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify);
    }

    public string Redact(string text)
    {
        foreach (var (pattern, variable) in _replacements.Value)
        {
            text = pattern.Replace(text, variable);
        }

        return text;
    }

    private static Regex CreatePattern(string folderPath)
    {
        var segments = folderPath.Split(s_separators, StringSplitOptions.RemoveEmptyEntries).Select(Regex.Escape);
        var pattern = string.Join(AnySeparator, segments) + SegmentEnd;

        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    // A $ in a replacement string would start a substitution such as $1.
    private static string EscapeReplacement(string variable) => variable.Replace("$", "$$", StringComparison.Ordinal);
}
