using System;
using Microsoft.Extensions.Logging;

namespace Bravo.Desktop.Diagnostics;

internal sealed partial class CrashReporting
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Critical,
        Message = "Bravo ends because of an exception that nothing caught. Report: {ReportFile}.")]
    private static partial void LogCrashed(ILogger logger, Exception exception, string reportFile);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "The report of the crash cannot be written in {Folder}.")]
    private static partial void LogReportNotWritten(ILogger logger, Exception exception, string folder);
}
