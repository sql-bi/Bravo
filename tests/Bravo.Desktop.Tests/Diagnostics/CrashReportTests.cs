using System;
using System.Linq;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Telemetry;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class CrashReportTests
{
    private const string Trace = "4bf92f3577b34da6a3ce929d0e0e4736";

    private static readonly DateTimeOffset s_time = new(2026, 11, 22, 9, 30, 15, TimeSpan.Zero);
    private static readonly PersonalNames s_names = new(@"C:\Users\jdoe", "jdoe", "LAPTOP-42");
    private static readonly TelemetrySession s_session = new("session-of-the-test");

    [Fact]
    public void Create_ExceptionWithACause_KeepsTheTextOfTheRuntimeWithTheCauseAndTheStacks()
    {
        var exception = Thrown(new InvalidOperationException("It failed.", Thrown(new TimeoutException("Too slow."))));

        var report = CrashReport.Create(exception, "1.1.0", s_session, s_time, Trace, "Bravo-20261122-093000-42.log", s_names);

        Assert.Equal(("System.InvalidOperationException", "It failed."), (report.Exception.Type, report.Exception.Message));
        Assert.Equal(exception.ToString().ReplaceLineEndings("\n").Split('\n'), report.Exception.Text);
        Assert.Contains(report.Exception.Text, line => line.Contains("System.TimeoutException: Too slow.", StringComparison.Ordinal));
        Assert.Contains(report.Exception.Text, line => line.TrimStart().StartsWith("at ", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_MessageWithPersonalNames_LeavesThemOut()
    {
        var exception = new UnauthorizedAccessException(@"Access to the path 'C:\Users\jdoe\Documents' is denied on LAPTOP-42.");

        var report = CrashReport.Create(exception, "1.1.0", s_session, s_time, Trace, logFile: null, s_names);

        var expected = @"Access to the path '%USERPROFILE%\Documents' is denied on %COMPUTERNAME%.";
        Assert.Equal(expected, report.Exception.Message);
        Assert.Contains(report.Exception.Text, line => line.Contains(expected, StringComparison.Ordinal));
        Assert.Equal(expected, Assert.Single(report.Exception.Chain!).Message);
    }

    // The reporter sends the crash from the report alone, as the telemetry of the run it belongs to.
    [Fact]
    public void Create_ExceptionWithACause_KeepsTheChainAsTheTelemetrySendsItAndTheRun()
    {
        var exception = Thrown(new InvalidOperationException("It failed.", Thrown(new TimeoutException("Too slow."))));

        var report = CrashReport.Create(exception, "1.1.0", s_session, s_time, Trace, logFile: null, s_names);

        Assert.Equal("session-of-the-test", report.Session);
        var chain = report.Exception.Chain!;
        Assert.Equal(
            [(1, 0, "System.InvalidOperationException", "It failed."), (2, 1, "System.TimeoutException", "Too slow.")],
            chain.Select(link => (link.Id, link.OuterId, link.TypeName, link.Message)));
        Assert.All(chain, link => Assert.NotEmpty(link.Frames));
    }

    // The reporter sends what the host would send; the text, for the user and for support, keeps the aggregate.
    [Fact]
    public void Create_AggregateOfOneException_StartsTheChainWithThatException()
    {
        var exception = new AggregateException(Thrown(new TimeoutException("Too slow.")));

        var report = CrashReport.Create(exception, "1.1.0", s_session, s_time, Trace, logFile: null, s_names);

        var first = report.Exception.Chain![0];
        Assert.Equal(("System.TimeoutException", "Too slow."), (first.TypeName, first.Message));
        Assert.Equal("System.AggregateException", report.Exception.Type);
    }

    private static Exception Thrown(Exception exception)
    {
        try
        {
            throw exception;
        }
        catch (Exception caught)
        {
            return caught;
        }
    }
}
