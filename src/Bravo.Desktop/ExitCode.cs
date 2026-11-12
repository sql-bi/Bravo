namespace Bravo.Desktop;

/// <summary>
/// The exit codes of the process. An exception that escapes ends the process with the code of the runtime.
/// </summary>
internal static class ExitCode
{
    /// <summary>
    /// The application ran and was closed, or this process handed its start over to the running instance.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Another process owns the instance and this start could not be handed over to it.
    /// </summary>
    public const int ActivationNotDelivered = 1;

    /// <summary>
    /// The files of the user interface are not beside the executable.
    /// </summary>
    public const int UserInterfaceMissing = 2;

    /// <summary>
    /// The WebView2 runtime is installed, and the webview could not be created with it.
    /// </summary>
    public const int WebViewUnavailable = 3;

    /// <summary>
    /// The processes of the webview ended more times in a row than the application recovers from.
    /// </summary>
    public const int WebViewFailed = 4;

    /// <summary>
    /// The WebView2 runtime is not installed. The code is the one that 1.0.x ends with in the same case.
    /// </summary>
    public const int WebViewRuntimeMissing = 1223;
}
