namespace Bravo.Desktop.WebView;

/// <summary>
/// What can go wrong with the webview, as the rules of <see cref="WebViewFailurePolicy"/> tell it apart.
/// </summary>
internal enum WebViewFailure
{
    /// <summary>
    /// The files of the user interface are not in the content folder.
    /// </summary>
    ContentMissing,

    /// <summary>
    /// The runtime of the engine is not installed.
    /// </summary>
    RuntimeMissing,

    /// <summary>
    /// The engine or its control could not be created: a runtime that is damaged or too old, a data folder
    /// that cannot be used.
    /// </summary>
    StartFailed,

    /// <summary>
    /// The process that renders the page ended.
    /// </summary>
    RenderProcessExited,

    /// <summary>
    /// The browser process ended, and with it every process of the engine and the control.
    /// </summary>
    BrowserProcessExited,

    /// <summary>
    /// Something that the engine recovers from by itself, such as the end of its graphics process, or that
    /// may pass, such as a page that does not answer.
    /// </summary>
    Transient,
}
