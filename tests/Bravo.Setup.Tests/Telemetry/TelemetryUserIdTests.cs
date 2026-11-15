using Bravo.Setup.Telemetry;
using Xunit;

namespace Bravo.Setup.Tests.Telemetry;

public sealed class TelemetryUserIdTests
{
    // The first value is the one that the tests of the installer of 1.0.x expect for the same account. The
    // second is the SHA-256 of the UTF-8 bytes of "PC-01\José", in lower case hexadecimal digits.
    [Theory]
    [InlineData("LAPTOP-12C9A7VU", "SYSTEM", "e4f87d099028e128ea2b413f4fa6fc741426bef314de588b0920e89f22015bd0")]
    [InlineData("PC-01", "José", "96261479daabd935414895fb48c143ec3d653fc69f0f58d5b108aa2a17784805")]
    public void Create_ReturnsTheHashThatVersion10Computes(string machineName, string userName, string expected)
    {
        var userId = TelemetryUserId.Create(machineName, userName);

        Assert.Equal(expected, userId);
    }

    [Fact]
    public void Create_TwoAccountsOfOneMachine_ReturnsTwoIdentifiers()
    {
        var first = TelemetryUserId.Create("DESKTOP-01", "anna");
        var second = TelemetryUserId.Create("DESKTOP-01", "bruno");

        Assert.NotEqual(first, second);
    }
}
