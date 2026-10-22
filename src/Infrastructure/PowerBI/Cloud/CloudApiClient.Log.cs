using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud;

internal partial class CloudApiClient
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Workspaces received from the Power BI service: {Response}")]
    private partial void LogWorkspacesReceived(string response);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Shared models received from the Power BI service: {Response}")]
    private partial void LogSharedModelsReceived(string response);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Workspaces read from the response: {Workspaces}")]
    private partial void LogWorkspacesRead(string workspaces);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Shared models read from the response: {SharedModels}")]
    private partial void LogSharedModelsRead(string sharedModels);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Datasets joined from workspaces and shared models: {Datasets}")]
    private partial void LogDatasetsJoined(string datasets);
}
