using System;
using Bravo.Core.Updates;
using Bravo.Desktop.Telemetry;
using Xunit;

namespace Bravo.Desktop.Tests.Telemetry;

public sealed class TelemetryContextTests
{
    private static readonly TelemetrySession s_session = new("session-of-the-test");

    // The vectors of the tests of 1.0.x: the same user on the same computer has the same identifier in every
    // version.
    [Theory]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("Bravo", "8123f58e72483f148509ae2da7feda62076dbe2ae3a045323bea4458a62d0952")]
    [InlineData(@"LAPTOP-12C9A7VU\SYSTEM", "e4f87d099028e128ea2b413f4fa6fc741426bef314de588b0920e89f22015bd0")]
    public void Hash_IsTheHashOf10x(string text, string expected)
    {
        Assert.Equal(expected, TelemetryContext.Hash(text));
    }

    [Fact]
    public void Create_NamesTheUserAndTheComputerByTheirHashes()
    {
        var context = TelemetryContext.Create(BuildIdentity.Development("1.1.0"), s_session, Installation.None, null);

        Assert.Equal(TelemetryContext.Hash($@"{Environment.MachineName}\{Environment.UserName}"), context.UserId);
        Assert.Equal(TelemetryContext.Hash(Environment.MachineName), context.RoleInstance);
        Assert.Equal(s_session.Id, context.SessionId);
    }

    [Fact]
    public void Create_ReleaseBuild_HasThePropertiesOf10x()
    {
        var identity = BuildIdentity.Release("1.1.0") with { FileVersion = new Version(1, 1, 0, 7), IncludesRuntime = true };

        var context = TelemetryContext.Create(identity, s_session, Installation.PerUser, "131.0.2903.70");

        Assert.Equal("1.1.0", context.Version);
        Assert.Equal("Bravo", context.Properties["ProductName"]);
        Assert.Equal("1.1.0", context.Properties["Version"]);
        Assert.Equal("1.1.0.7", context.Properties["Build"]);
        Assert.Equal("SelfContained", context.Properties["PublishMode"]);
        Assert.Equal("PerUser", context.Properties["InstallScope"]);
        Assert.Equal("131.0.2903.70", context.Properties["WebView2Version"]);
    }

    [Fact]
    public void ForPage_GivesThePageTheValuesOfTheHost()
    {
        var context = TelemetryContext.Create(BuildIdentity.Development("1.1.0"), s_session, Installation.PerMachine, "131.0.2903.70");

        var page = context.ForPage("InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:9");

        Assert.Equal("InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:9", page.ConnectionString);
        Assert.Equal(context.UserId, page.UserId);
        Assert.Equal(s_session.Id, page.SessionId);
        Assert.Equal("1.1.0", page.Version);
        Assert.Equal(context.Properties, page.Properties);
    }

    [Theory]
    [InlineData(false, false, "FrameworkDependent")]
    [InlineData(false, true, "SelfContained")]
    [InlineData(true, true, "None")]
    public void Create_PublishMode_IsTheValueOf10x(bool isDevelopmentBuild, bool includesRuntime, string expected)
    {
        var build = isDevelopmentBuild ? BuildIdentity.Development("1.1.0") : BuildIdentity.Release("1.1.0");
        var identity = build with { IncludesRuntime = includesRuntime };

        var context = TelemetryContext.Create(identity, s_session, Installation.None, null);

        Assert.Equal(expected, context.Properties["PublishMode"]);
    }

    [Theory]
    [InlineData(Installation.None, "Portable")]
    [InlineData(Installation.PerUser, "PerUser")]
    [InlineData(Installation.PerMachine, "PerMachine")]
    public void Create_InstallScope_IsTheValueOf10x(Installation installation, string expected)
    {
        var context = TelemetryContext.Create(BuildIdentity.Development("1.1.0"), s_session, installation, null);

        Assert.Equal(expected, context.Properties["InstallScope"]);
    }

    [Fact]
    public void Create_WithoutTheRuntimeOfTheWebview_SendsAnEmptyVersion()
    {
        var context = TelemetryContext.Create(BuildIdentity.Development("1.1.0"), s_session, Installation.None, null);

        Assert.Equal(string.Empty, context.Properties["WebView2Version"]);
    }
}
