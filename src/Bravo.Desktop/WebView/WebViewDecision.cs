namespace Bravo.Desktop.WebView;

/// <summary>
/// What <see cref="WebViewFailurePolicy"/> decided for a failure.
/// </summary>
/// <param name="Recovery">What to do.</param>
/// <param name="ExitCode">
/// The exit code of the process when <paramref name="Recovery"/> is <see cref="WebViewRecovery.End"/>.
/// </param>
internal readonly record struct WebViewDecision(WebViewRecovery Recovery, int ExitCode)
{
    public static WebViewDecision Continue(WebViewRecovery recovery)
    {
        return new WebViewDecision(recovery, Desktop.ExitCode.Success);
    }

    public static WebViewDecision End(int exitCode)
    {
        return new WebViewDecision(WebViewRecovery.End, exitCode);
    }
}
