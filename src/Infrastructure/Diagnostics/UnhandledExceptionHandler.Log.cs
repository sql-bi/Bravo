using System;
using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

internal sealed partial class UnhandledExceptionHandler
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "An unhandled exception occurred. Origin: {Origin}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, ExceptionOrigin origin);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "The error report dialog could not be shown.")]
    private static partial void LogDialogFailed(ILogger logger, Exception exception);
}
