using Microsoft.Web.WebView2.Core;

namespace Bravo.Desktop.WebView;

/// <summary>
/// The WebView2 runtime that the webview of this process would use.
/// </summary>
internal static class WebViewRuntime
{
    /// <summary>
    /// Its version, as its loader reads it, or null when no runtime is installed.
    /// </summary>
    public static string? GetVersion()
    {
        try
        {
            return CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch (WebView2RuntimeNotFoundException)
        {
            return null;
        }
    }
}
