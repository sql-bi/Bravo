using System;
using Bravo.Core.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Bravo.Api.Problems;

internal sealed partial class BravoExceptionHandler
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "The request {Method} {Path} ended with the error {Code}.")]
    private partial void LogForeseenError(Exception exception, string method, PathString path, ErrorCode code);
}
