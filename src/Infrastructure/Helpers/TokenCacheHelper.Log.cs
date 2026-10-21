using System;
using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Infrastructure.Helpers;

internal static partial class TokenCacheHelper
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "The token cache could not be decrypted and is deleted: the user signs in again.")]
    private static partial void LogTokenCacheUnreadable(ILogger logger, Exception exception);
}
