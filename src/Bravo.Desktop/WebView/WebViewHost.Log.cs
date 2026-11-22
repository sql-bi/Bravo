using Microsoft.Extensions.Logging;

namespace Bravo.Desktop.WebView;

internal sealed partial class WebViewHost
{
    // At the start, and at each recovery that creates the engine again.
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "The webview runs on the WebView2 runtime {Version}.")]
    private partial void LogEngineCreated(string version);
}
