#if DEBUG
using System;
using Sqlbi.Bravo.Host;
using Xunit;

namespace Bravo.Tests.Host;

public class CrashTestTests
{
    [Theory]
    [InlineData("ui", nameof(CrashTestScenario.Ui))]
    [InlineData("StackOverflow", nameof(CrashTestScenario.StackOverflow))]
    public void Extract_ReadsTheScenarioWithoutCase(string value, string scenario)
    {
        var extracted = CrashTest.Extract(["--crash-test", value], out _);

        Assert.Equal(scenario, extracted?.ToString());
    }

    [Fact]
    public void Extract_RemovesTheOptionAndItsScenario()
    {
        var scenario = CrashTest.Extract(["--server", "localhost:1234", "--crash-test", "thread", "--database", "Sales"], out var remainingArgs);

        Assert.Equal(nameof(CrashTestScenario.Thread), scenario?.ToString());
        Assert.Equal(["--server", "localhost:1234", "--database", "Sales"], remainingArgs);
    }

    [Fact]
    public void Extract_ReturnsNullAndEveryArgumentWithoutTheOption()
    {
        var scenario = CrashTest.Extract(["--server", "localhost:1234"], out var remainingArgs);

        Assert.Null(scenario);
        Assert.Equal(["--server", "localhost:1234"], remainingArgs);
    }

    [Theory]
    [InlineData("crash")]
    [InlineData("1")]
    public void Extract_RejectsAnUnknownScenario(string value)
    {
        Assert.Throws<ArgumentException>(() => CrashTest.Extract(["--crash-test", value], out _));
    }

    [Fact]
    public void Extract_RejectsTheOptionWithoutAScenario()
    {
        Assert.Throws<ArgumentException>(() => CrashTest.Extract(["--crash-test"], out _));
    }
}
#endif
