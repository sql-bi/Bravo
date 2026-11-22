using System;
using Microsoft.Extensions.Logging;

namespace Bravo.Desktop.Diagnostics;

internal sealed partial class HostLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "The file of the log cannot be created in '{Folder}': this run is recorded without it.")]
    private static partial void LogFileUnavailable(ILogger logger, Exception exception, string folder);
}
