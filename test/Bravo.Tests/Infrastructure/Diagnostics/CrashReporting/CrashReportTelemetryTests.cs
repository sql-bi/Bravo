using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics.CrashReporting;

public class CrashReportTelemetryTests
{
    [Fact]
    public void Create_TracksTheExceptionOfTheReport()
    {
        var telemetry = CrashReportTelemetry.Create(TestCrashReports.Create());

        var details = Assert.Single(telemetry.ExceptionDetailsInfoList);
        Assert.Equal("System.InvalidOperationException", details.TypeName);
        Assert.Equal("outer", details.Message);
        Assert.Equal("System.InvalidOperationException", telemetry.ProblemId);
    }

    [Fact]
    public void Create_KeepsTheTimeAndTheSessionOfTheCrash()
    {
        var telemetry = CrashReportTelemetry.Create(TestCrashReports.Create());

        Assert.Equal(TestCrashReports.Timestamp, telemetry.Timestamp);
        Assert.Equal(TestCrashReports.SessionId, telemetry.Context.Session.Id);
    }

    [Fact]
    public void Create_MarksTheExceptionUnhandled()
    {
        var telemetry = CrashReportTelemetry.Create(TestCrashReports.Create());

        Assert.Equal("Unhandled", telemetry.Properties["ExceptionKind"]);
        Assert.Equal(nameof(ExceptionOrigin.AppDomain), telemetry.Properties["ExceptionOrigin"]);
    }
}
