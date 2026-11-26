using Bravo.Desktop.Development;
using Bravo.Desktop.Diagnostics;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class CrashReporterTests
{
    private static readonly BuildIdentity s_release = BuildIdentity.Release("1.1.0");
    private static readonly BuildIdentity s_development = BuildIdentity.Development("1.1.0");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IsRequested_SwitchAndPath_ReturnsThePath(bool isDevelopmentBuild)
    {
        var identity = isDevelopmentBuild ? s_development : s_release;

        var isRequested = CrashReporter.IsRequested(["--crash-report", @"C:\Data\Logs\Bravo-1.crash.json"], identity, out var start);

        Assert.True(isRequested);
        Assert.Equal(new CrashReporterStart(@"C:\Data\Logs\Bravo-1.crash.json", DevelopmentOptions.None), start);
    }

    // The external tool of Power BI Desktop starts the application with its own arguments: never the reporter.
    [Theory]
    [InlineData]
    [InlineData("--crash-report")]
    [InlineData("--server=localhost:12345", "--database=db")]
    [InlineData("--crash-report", "report.json", "--server=localhost:12345")]
    [InlineData("--server=localhost:12345", "--crash-report", "report.json")]
    [InlineData("--crash-report", "report.json", "--dev-data-folder", @"C:\Data")]
    public void IsRequested_OtherCommandLine_ReturnsFalse(params string[] args)
    {
        Assert.False(CrashReporter.IsRequested(args, s_release, out _));
        Assert.False(CrashReporter.IsRequested(args, s_development, out _));
    }

    [Fact]
    public void IsRequested_ReleaseBuildWithTheOptionsOfADevelopmentBuild_ReturnsFalse()
    {
        string[] args = ["--crash-report", "report.json", @"--dev-data-folder=C:\Data"];

        Assert.False(CrashReporter.IsRequested(args, s_release, out _));
    }

    [Fact]
    public void CreateArguments_NoDevelopmentOptions_AreTheSwitchAndThePath()
    {
        Assert.Equal(["--crash-report", "report.json"], CrashReporter.CreateArguments("report.json", DevelopmentOptions.None));
    }

    // The reporter of a development build finds the data folder, the policies and the endpoint of the process
    // that crashed, and nothing else of its command line.
    [Fact]
    public void CreateArguments_DevelopmentOptions_AreReadBackByTheReporter()
    {
        var development = new DevelopmentOptions
        {
            AllowedOrigin = "http://127.0.0.1:5173",
            DataFolderPath = @"C:\Data\Bravo tests",
            PolicyKeyName = @"Software\Bravo.Tests\Policies",
            TelemetryEndpoint = "http://127.0.0.1:8097",
            Crash = DevelopmentCrash.Window,
        };

        var args = CrashReporter.CreateArguments(@"C:\Data\Bravo tests\Logs\Bravo-1.crash.json", development);
        var isRequested = CrashReporter.IsRequested([.. args], s_development, out var start);

        Assert.True(isRequested);
        var expected = new DevelopmentOptions
        {
            DataFolderPath = development.DataFolderPath,
            PolicyKeyName = development.PolicyKeyName,
            TelemetryEndpoint = development.TelemetryEndpoint,
        };
        Assert.Equal(new CrashReporterStart(@"C:\Data\Bravo tests\Logs\Bravo-1.crash.json", expected), start);
    }
}
