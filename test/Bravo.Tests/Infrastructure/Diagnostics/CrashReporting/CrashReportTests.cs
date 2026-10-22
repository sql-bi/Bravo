using System;
using System.IO;
using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics.CrashReporting;

public class CrashReportTests
{
    private const string ProfilePath = @"C:\Users\jane.doe";

    private static readonly DateTimeOffset s_now = new(2026, 10, 22, 8, 30, 15, TimeSpan.Zero);
    private static readonly UserProfilePathRedactor s_redactor = new([new(ProfilePath, "%USERPROFILE%")]);

    [Fact]
    public void Create_KeepsTheExceptionWithItsInnerExceptions()
    {
        var exception = new InvalidOperationException("outer", new TimeoutException("inner"));

        var report = Create(exception);

        Assert.Equal("System.InvalidOperationException", report.ExceptionType);
        Assert.Equal("outer", report.ExceptionMessage);
        Assert.Contains("System.TimeoutException: inner", report.ExceptionText);
    }

    [Fact]
    public void Create_RecordsTheSessionTheTelemetryConsentAndTheTime()
    {
        var report = Create(new InvalidOperationException());

        Assert.Equal(TestCrashReports.SessionId, report.SessionId);
        Assert.True(report.TelemetryAllowed);
        Assert.Equal(s_now, report.Timestamp);
    }

    [Fact]
    public void Create_RedactsTheFoldersOfTheUserProfile()
    {
        var exception = new FileNotFoundException($@"Could not find {ProfilePath}\Sales.pbix.");

        var report = Create(exception);

        Assert.Equal(@"Could not find %USERPROFILE%\Sales.pbix.", report.ExceptionMessage);
        Assert.DoesNotContain(ProfilePath, report.ExceptionText);
    }

    [Fact]
    public void Create_RedactsTheSensitiveTags()
    {
        var exception = new InvalidOperationException("Cannot connect to <pii>sales-server</pii>.");

        var report = Create(exception);

        Assert.Equal("Cannot connect to <pii>[REDACTED]</pii>.", report.ExceptionMessage);
        Assert.DoesNotContain("sales-server", report.ExceptionText);
    }

    private static CrashReport Create(Exception exception)
    {
        return CrashReport.Create(exception, TestCrashReports.SessionId, telemetryAllowed: true, s_now, s_redactor);
    }
}
