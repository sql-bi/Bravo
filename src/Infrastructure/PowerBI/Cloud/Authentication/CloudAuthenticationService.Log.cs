using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Authentication;

internal partial class CloudAuthenticationService
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Signing in to the Power BI environment: {Environment}")]
    private partial void LogSigningIn(string environment);
}
