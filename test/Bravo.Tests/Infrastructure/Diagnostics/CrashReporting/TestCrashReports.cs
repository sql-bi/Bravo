using System;
using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;

namespace Bravo.Tests.Infrastructure.Diagnostics.CrashReporting;

internal static class TestCrashReports
{
    public const string SessionId = "8f9e0c1a-1111-2222-3333-444455556666";

    public static CrashReport Create(DateTimeOffset timestamp)
    {
        return new CrashReport(
            timestamp,
            "1.1.0+abcdef",
            SessionId,
            TelemetryAllowed: true,
            "System.InvalidOperationException",
            "outer",
            "System.InvalidOperationException: outer\r\n ---> System.TimeoutException: inner\r\n   --- End of inner exception stack trace ---");
    }
}
