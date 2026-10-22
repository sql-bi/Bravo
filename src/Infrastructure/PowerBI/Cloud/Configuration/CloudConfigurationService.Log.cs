using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Configuration;

internal sealed partial class CloudConfigurationService
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Environment discovery {ApiVersion} response received: {Response}")]
    private partial void LogEnvironmentsReceived(string apiVersion, string response);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Tenant cluster response received: {Response}")]
    private partial void LogTenantClusterReceived(string response);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Power BI environments discovered for the account: {Environments}")]
    private partial void LogEnvironmentsDiscovered(string environments);
}
