using System;
using Bravo.Api.Diagnostics;
using Bravo.Desktop.Diagnostics;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class LogFileLineTests
{
    private static readonly LogRecord s_record = new()
    {
        Number = 7,
        Time = new DateTimeOffset(2026, 11, 22, 10, 30, 15, 123, TimeSpan.FromHours(1)),
        Level = LogRecordLevel.Warning,
        Category = "Bravo.Api.Problems.BravoExceptionHandler",
        EventId = 1,
        Message = "The request GET /releases/latest ended with the error UpdateCheckFailed.",
        TraceId = "4bf92f3577b34da6a3ce929d0e0e4736",
    };

    [Fact]
    public void Format_Record_IsOneLineWithTheTimeInUtcFirst()
    {
        var line = LogFileLine.Format(s_record);

        Assert.Equal(
            "2026-11-22T09:30:15.123Z Warning 4bf92f3577b34da6a3ce929d0e0e4736 Bravo.Api.Problems.BravoExceptionHandler[1] "
            + "The request GET /releases/latest ended with the error UpdateCheckFailed.",
            line);
    }

    [Fact]
    public void Format_RecordWithoutATrace_HasADashInItsPlace()
    {
        var line = LogFileLine.Format(s_record with { TraceId = null });

        Assert.Contains(" Warning - Bravo.Api.Problems.BravoExceptionHandler[1] ", line, StringComparison.Ordinal);
    }

    // A line that is not indented begins a record: the other lines of a record are indented.
    [Fact]
    public void Format_MessageAndExceptionOnMoreLines_IndentsEveryLineAfterTheFirst()
    {
        var record = s_record with { Message = "First\nSecond", Exception = "System.Exception: Failed\n   at Bravo.Run()" };

        var line = LogFileLine.Format(record);

        Assert.EndsWith("First\r\n    Second\r\n    System.Exception: Failed\r\n       at Bravo.Run()", line, StringComparison.Ordinal);
    }
}
