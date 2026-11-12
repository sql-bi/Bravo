namespace Bravo.Desktop.WebView;

/// <summary>
/// What the application decides about its webview. The webview computes none of these.
/// </summary>
internal sealed record WebViewOptions
{
    /// <summary>
    /// The folder where the engine keeps its own files and what the page stores. The engine creates it.
    /// </summary>
    public required string UserDataFolder { get; init; }

    /// <summary>
    /// The folder with the files of the user interface, which the page is served from.
    /// </summary>
    public required string ContentFolder { get; init; }

    /// <summary>
    /// Whether the developer tools of the engine, and the keys of a browser that open them, are available.
    /// </summary>
    public required bool AreDeveloperToolsEnabled { get; init; }
}
