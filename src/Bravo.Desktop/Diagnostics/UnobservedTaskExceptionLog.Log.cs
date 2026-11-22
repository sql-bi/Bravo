using System;
using Microsoft.Extensions.Logging;

namespace Bravo.Desktop.Diagnostics;

internal sealed partial class UnobservedTaskExceptionLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "A task failed, and nothing awaited it.")]
    private static partial void LogUnobservedTaskException(ILogger logger, Exception exception);
}
