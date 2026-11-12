using System;

namespace Bravo.Desktop.WebView;

/// <summary>
/// Where the page of the user interface is, for the webview that serves it and for the API that it calls.
/// The host name ends with a name reserved for what is no site: the webview answers it with the files of a
/// folder, so the origin of the page is the same at every start.
/// </summary>
internal static class PageAddress
{
    public const string HostName = "bravo.invalid";

    /// <summary>
    /// The origin of the page: what the API allows, and what the connection to the API is given to.
    /// </summary>
    public const string Origin = "https://" + HostName;

    /// <summary>
    /// The file of the content folder that the window opens.
    /// </summary>
    public const string IndexFileName = "index.html";

    public const string Start = Origin + "/" + IndexFileName;

    /// <summary>
    /// Whether an address is of the origin of the page.
    /// </summary>
    public static bool Contains(string? address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps
            && uri.IsDefaultPort
            && string.Equals(uri.IdnHost, HostName, StringComparison.OrdinalIgnoreCase);
    }
}
