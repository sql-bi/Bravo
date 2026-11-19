using System;
using System.Text.Json;
using Bravo.Core.Errors;
using Bravo.Core.Updates;

namespace Bravo.Platform.Windows.Updates;

/// <summary>
/// The update feed as installed copies of every version read it: a JSON object whose members <c>version</c>,
/// <c>download</c> and <c>changelog</c> are text. Other members are ignored.
/// </summary>
internal static class ReleaseFeedDocument
{
    /// <exception cref="BravoException">
    /// With <see cref="ErrorCode.UpdateCheckFailed"/>: the text is not JSON, or names no release.
    /// </exception>
    public static PublishedRelease Parse(string text)
    {
        var release = TryParse(text);
        if (release is null)
        {
            throw new BravoException(ErrorCode.UpdateCheckFailed, "The update feed names no release.");
        }

        return release;
    }

    private static PublishedRelease? TryParse(string text)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!Version.TryParse(ReadText(root, "version"), out var version))
            {
                return null;
            }

            var download = ReadAddress(ReadText(root, "download"));
            var releaseNotes = ReadAddress(ReadText(root, "changelog"));
            if (download is null || releaseNotes is null)
            {
                return null;
            }

            return new PublishedRelease(version, download, releaseNotes);
        }
    }

    private static string? ReadText(JsonElement root, string name)
    {
        var isText = root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String;

        return isText ? value.GetString() : null;
    }

    private static Uri? ReadAddress(string? text)
    {
        if (!Uri.TryCreate(text, UriKind.Absolute, out var address))
        {
            return null;
        }

        var isWebAddress = address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp;

        return isWebAddress ? address : null;
    }
}
