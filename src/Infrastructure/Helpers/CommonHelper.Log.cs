using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Infrastructure.Helpers;

internal static partial class CommonHelper
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Update feed received: {Feed}")]
    private static partial void LogUpdateFeedReceived(ILogger logger, string feed);
}
