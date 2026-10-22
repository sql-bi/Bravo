using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Infrastructure;

internal partial class AppWindow
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "WebView2 additional browser arguments: {Arguments}")]
    private partial void LogBrowserArguments(string? arguments);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Environment diagnostics: {Diagnostics}")]
    private partial void LogEnvironmentDiagnostics(string diagnostics);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Startup message created from the command line: {StartupMessage}")]
    private partial void LogStartupMessageCreated(string startupMessage);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Posting the web message: {WebMessage}")]
    private partial void LogWebMessagePosting(string? webMessage);
}
