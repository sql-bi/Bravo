using System;
using Microsoft.Extensions.Logging;

namespace Sqlbi.Bravo.Models;

public partial class PBIDesktopReport
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "The model of Power BI Desktop process {ProcessId} could not be reached to find its connection mode.")]
    private static partial void LogConnectionModeUnavailable(ILogger logger, int? processId, Exception exception);
}
