using System;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace Bravo.Desktop.WebView;

internal sealed partial class WebViewHost
{
    // At the start, and at each recovery that creates the engine again.
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "The webview runs on the WebView2 runtime {Version}.")]
    private partial void LogEngineCreated(string version);

    [LoggerMessage(
        EventId = 2,
        Message = "A process of the webview failed: {Kind}, because of {Reason}, with the exit code {ExitCode}. Process: {Process}.")]
    private partial void LogProcessFailed(
        LogLevel level,
        CoreWebView2ProcessFailedKind kind,
        CoreWebView2ProcessFailedReason reason,
        int exitCode,
        string process);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "The webview recovers from {Failure}: {Recovery}.")]
    private partial void LogRecovering(WebViewFailure failure, WebViewRecovery recovery);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "The webview cannot go on after {Failure}: the application ends with the exit code {ExitCode}.")]
    private partial void LogEnded(Exception? exception, WebViewFailure failure, int exitCode);
}
