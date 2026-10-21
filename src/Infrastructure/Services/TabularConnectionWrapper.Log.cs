using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Infrastructure.Services;

internal partial class TabularConnectionWrapper
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Database {DatabaseIdOrName} not found (by ID: {FindById}) among the databases of the server: {ServerDatabases}")]
    private static partial void LogDatabaseNotFound(ILogger logger, string databaseIdOrName, bool findById, string serverDatabases);
}
