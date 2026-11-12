using System;

namespace Bravo.Desktop.WebView;

/// <summary>
/// A failure of the webview that the application does not recover from.
/// </summary>
internal sealed class WebViewEndedEventArgs(WebViewFailure failure, int exitCode, Exception? error) : EventArgs
{
    public WebViewFailure Failure { get; } = failure;

    /// <summary>
    /// The exit code that the process ends with.
    /// </summary>
    public int ExitCode { get; } = exitCode;

    /// <summary>
    /// What the engine reported, when the failure came as an exception.
    /// </summary>
    public Exception? Error { get; } = error;
}
