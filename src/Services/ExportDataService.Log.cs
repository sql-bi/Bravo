using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Services;

internal partial class ExportDataService
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Exporting table {TableName} to {Format}, row batch mode: {RowBatchMode}.")]
    private partial void LogTableExporting(string? tableName, string format, bool rowBatchMode);
}
