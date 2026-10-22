using System;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Bravo.Tests.Infrastructure.Diagnostics;

internal sealed class RecordingDialog : IErrorReportDialog
{
    public int ShowCount { get; private set; }
    public ErrorReport? LastReport { get; private set; }
    public ITelemetryService? LastTelemetry { get; private set; }
    public Exception? ExceptionToThrow { get; init; }

    public void Show(ErrorReport report, ITelemetryService? telemetry)
    {
        ShowCount++;
        LastReport = report;
        LastTelemetry = telemetry;

        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    private readonly DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;
}

internal static class TestLogEntries
{
    public static DiagnosticLogEntry Create(string message, LogLevel level = LogLevel.Warning)
    {
        return new DiagnosticLogEntry(DateTimeOffset.UnixEpoch, level, "Test", 0, null, message, null, null, null);
    }
}
