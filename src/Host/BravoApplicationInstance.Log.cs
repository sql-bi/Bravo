using System;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.SingleInstance;

namespace Sqlbi.Bravo.Host;

internal sealed partial class BravoApplicationInstance
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "The single-instance listener reported a failure and keeps listening.")]
    private partial void LogListenerFailed(Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "The activation could not be redirected to the primary instance ({Status}).")]
    private partial void LogActivationRedirectFailed(Exception exception, SingleInstanceSendStatus status);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "The activation payload could not be read; the window is activated without a document.")]
    private partial void LogActivationPayloadUnreadable(Exception exception);
}
