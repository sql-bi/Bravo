using System;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Diagnostics;

namespace Bravo.Tests.Infrastructure.Diagnostics;

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
