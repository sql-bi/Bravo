using System;

namespace Bravo.Desktop.WebView;

/// <summary>
/// What the user reads when the application ends because of its webview.
/// </summary>
/// <param name="Heading">What happened, in one line.</param>
/// <param name="Text">What it means and what to do.</param>
/// <param name="LinkAddress">An address that the message offers to open, when there is one.</param>
internal sealed record WebViewFailureMessage(string Heading, string Text, string? LinkAddress)
{
    /// <summary>
    /// The installer that Microsoft offers for the Evergreen runtime: it downloads the runtime for the
    /// architecture of the machine.
    /// </summary>
    public const string RuntimeDownloadAddress = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

    private const string Runtime = "Microsoft Edge WebView2 Runtime";

    public static WebViewFailureMessage For(WebViewFailure failure, Exception? error)
    {
        return failure switch
        {
            WebViewFailure.ContentMissing => new WebViewFailureMessage(
                $"The user interface of {Product.Name} is missing.",
                $"Some files of the installation were not found. Install {Product.Name} again.",
                LinkAddress: null),
            WebViewFailure.RuntimeMissing => new WebViewFailureMessage(
                $"{Product.Name} needs the {Runtime}.",
                $"The runtime shows the user interface of {Product.Name}, and it is not installed on this "
                + $"computer. Download it from Microsoft, run the installer, then start {Product.Name} again.",
                RuntimeDownloadAddress),
            WebViewFailure.StartFailed => new WebViewFailureMessage(
                $"{Product.Name} could not start the {Runtime}.",
                "The runtime, which shows the user interface, may be damaged or too old, or its data folder "
                + $"cannot be used.{Describe(error)}",
                LinkAddress: null),
            WebViewFailure.RenderProcessExited or WebViewFailure.BrowserProcessExited => new WebViewFailureMessage(
                $"The user interface of {Product.Name} keeps stopping.",
                $"The {Runtime} ended several times in a row, and {Product.Name} has to close. Start it "
                + "again; if it happens again, restart the computer.",
                LinkAddress: null),
            _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, message: null),
        };
    }

    private static string Describe(Exception? error)
    {
        if (error is null)
        {
            return string.Empty;
        }

        var separator = Environment.NewLine + Environment.NewLine;
        return $"{separator}{error.GetType().FullName}: {error.Message}";
    }
}
