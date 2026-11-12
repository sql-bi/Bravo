namespace Bravo.Desktop.WebView;

/// <summary>
/// What is done after a failure of the webview.
/// </summary>
internal enum WebViewRecovery
{
    /// <summary>
    /// Nothing: the engine recovers by itself.
    /// </summary>
    None,

    /// <summary>
    /// The page is loaded again in the same control.
    /// </summary>
    Reload,

    /// <summary>
    /// The control is disposed and a new one is created.
    /// </summary>
    Recreate,

    /// <summary>
    /// The user is told, and the application ends.
    /// </summary>
    End,
}
