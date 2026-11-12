using System;
using Microsoft.Extensions.Logging;

namespace Bravo.Platform.Windows.SingleInstance;

public sealed partial class HandOverListener
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "The pipe '{PipeName}' could not be created: a second start cannot reach this instance.")]
    private partial void LogPipeUnavailable(Exception exception, string pipeName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "The pipe failed while waiting for a second start, and is created again.")]
    private partial void LogConnectionFailed(Exception exception);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "A second start connected and did not complete its request.")]
    private partial void LogExchangeFailed(Exception exception);
}
