using System;
using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Infrastructure.Extensions;

internal static partial class HostingExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "The request model is not valid: {ValidationProblem}")]
    private static partial void LogRequestModelInvalid(ILogger logger, string validationProblem);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "The request failed with an exception mapped to a problem details response.")]
    private static partial void LogExceptionMapped(ILogger logger, Exception exception);
}
