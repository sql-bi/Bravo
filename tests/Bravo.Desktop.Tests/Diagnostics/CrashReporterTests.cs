using Bravo.Desktop.Diagnostics;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class CrashReporterTests
{
    [Fact]
    public void IsRequested_SwitchAndPath_ReturnsThePath()
    {
        var isRequested = CrashReporter.IsRequested(["--crash-report", @"C:\Data\Logs\Bravo-1.crash.json"], out var reportPath);

        Assert.True(isRequested);
        Assert.Equal(@"C:\Data\Logs\Bravo-1.crash.json", reportPath);
    }

    // The external tool of Power BI Desktop starts the application with its own arguments: never the reporter.
    [Theory]
    [InlineData]
    [InlineData("--crash-report")]
    [InlineData("--server=localhost:12345", "--database=db")]
    [InlineData("--crash-report", "report.json", "--server=localhost:12345")]
    [InlineData("--server=localhost:12345", "--crash-report", "report.json")]
    public void IsRequested_OtherCommandLine_ReturnsFalse(params string[] args)
    {
        Assert.False(CrashReporter.IsRequested(args, out _));
    }
}
