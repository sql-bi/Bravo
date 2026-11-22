using System;
using Bravo.Desktop.Diagnostics;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class CrashReportTests
{
    private const string Trace = "4bf92f3577b34da6a3ce929d0e0e4736";

    private static readonly DateTimeOffset s_time = new(2026, 11, 22, 9, 30, 15, TimeSpan.Zero);
    private static readonly PersonalNames s_names = new(@"C:\Users\jdoe", "jdoe", "LAPTOP-42");

    [Fact]
    public void Create_ExceptionWithACause_KeepsTheTextOfTheRuntimeWithTheCauseAndTheStacks()
    {
        var exception = Thrown(new InvalidOperationException("It failed.", Thrown(new TimeoutException("Too slow."))));

        var report = CrashReport.Create(exception, "1.1.0", s_time, Trace, "Bravo-20261122-093000-42.log", s_names);

        Assert.Equal(("System.InvalidOperationException", "It failed."), (report.Exception.Type, report.Exception.Message));
        Assert.Equal(exception.ToString().ReplaceLineEndings("\n").Split('\n'), report.Exception.Text);
        Assert.Contains(report.Exception.Text, line => line.Contains("System.TimeoutException: Too slow.", StringComparison.Ordinal));
        Assert.Contains(report.Exception.Text, line => line.TrimStart().StartsWith("at ", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_MessageWithPersonalNames_LeavesThemOut()
    {
        var exception = new UnauthorizedAccessException(@"Access to the path 'C:\Users\jdoe\Documents' is denied on LAPTOP-42.");

        var report = CrashReport.Create(exception, "1.1.0", s_time, Trace, logFile: null, s_names);

        var expected = @"Access to the path '%USERPROFILE%\Documents' is denied on %COMPUTERNAME%.";
        Assert.Equal(expected, report.Exception.Message);
        Assert.Contains(report.Exception.Text, line => line.Contains(expected, StringComparison.Ordinal));
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
