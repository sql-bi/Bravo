using System;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.SingleInstance;

namespace Sqlbi.Bravo.Host;

internal static partial class BravoSingleInstance
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "The activation could not be redirected to the primary instance ({Status}).")]
    private static partial void LogActivationRedirectFailed(ILogger logger, Exception exception, SingleInstanceSendStatus status);
}
