using Sqlbi.Bravo.Host;
using Xunit;

namespace Bravo.Tests.Host;

/// <summary>
/// Pins the instance name format. It is a frozen contract: a build that computes a different name
/// does not see instances started by another build, so single-instance silently stops working
/// across an upgrade. These assertions are meant to fail when the format is changed by accident.
/// </summary>
public class InstancePipeNameTests
{
    private const string UserSid = "S-1-5-21-1111111111-2222222222-3333333333-1001";

    [Fact]
    public void Create_UsesPlainNamedPipeNaming()
    {
        // No "LOCAL\" prefix: that is an AppContainer requirement, and every Bravo distribution
        // (Portable, the two MSI variants, the MSIX/Store package) is a full-trust desktop process,
        // not an AppContainer.
        var pipeName = InstancePipeName.Create(sessionId: 1, UserSid);

        Assert.Equal($"Bravo.8D4D9F1D39F94C7789D84729480D8198.1.{UserSid}", pipeName);
    }

    [Fact]
    public void Create_DifferentSession_IsADifferentInstance()
    {
        // One instance per session, so Remote Desktop Services users do not share one.
        var first = InstancePipeName.Create(sessionId: 1, UserSid);
        var second = InstancePipeName.Create(sessionId: 2, UserSid);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_DifferentUser_IsADifferentInstance()
    {
        // One instance per account: a "run as" with another user gets its own instance, because
        // that account has its own settings, MSAL cache and registry.
        var first = InstancePipeName.Create(sessionId: 1, UserSid);
        var second = InstancePipeName.Create(sessionId: 1, userSid: "S-1-5-18");

        Assert.NotEqual(first, second);
    }
}
