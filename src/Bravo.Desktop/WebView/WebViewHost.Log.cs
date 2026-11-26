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

    // The host only: the address may carry a path, a query or a user name.
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "The webview gave no credentials to {Server}, which asked for them: only the proxy server of the settings gets the ones that the user entered.")]
    private partial void LogCredentialsNotGiven(string server);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "The data folder of the webview is still in use by a browser process with other arguments: the webview is created again.")]
    private partial void LogDataFolderInUse();
}
