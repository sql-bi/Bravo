using System;
using System.Net;
using Bravo.Core.Network;
using Bravo.Core.Settings;
using NSubstitute;
using Xunit;

namespace Bravo.Core.Tests.Network;

public sealed class ProxyCredentialsTests
{
    private static readonly Uri s_proxy = new("http://proxy.bravo.test:8080/");

    private readonly IUserSettingsStore _settings = Substitute.For<IUserSettingsStore>();
    private readonly IProxyCredentialStore _store = Substitute.For<IProxyCredentialStore>();

    [Theory]
    [InlineData("Negotiate")]
    [InlineData("NTLM")]
    [InlineData("negotiate")]
    public void GetCredential_WindowsUserAndASchemeWithoutPassword_IsTheWindowsUser(string scheme)
    {
        GivenTheUserChose(useDefaultCredentials: true);

        var credential = CreateCredentials().GetCredential(s_proxy, scheme);

        Assert.Same(CredentialCache.DefaultNetworkCredentials, credential);
    }

    [Theory]
    [InlineData("Basic")]
    [InlineData("Digest")]
    public void GetCredential_WindowsUserAndASchemeThatSendsAPassword_IsNone(string scheme)
    {
        GivenTheUserChose(useDefaultCredentials: true);

        var credential = CreateCredentials().GetCredential(s_proxy, scheme);

        Assert.Null(credential);
    }

    // No test of a developer machine reaches the store of Windows while the settings are the default ones.
    [Fact]
    public void GetCredential_WindowsUser_DoesNotReadTheStore()
    {
        GivenTheUserChose(useDefaultCredentials: true);
        var credentials = CreateCredentials();

        _ = credentials.GetCredential(s_proxy, "Basic");
        _ = credentials.GetCredential(s_proxy, "Negotiate");

        _ = _store.DidNotReceive().Read();
    }

    [Theory]
    [InlineData("Basic")]
    [InlineData("Digest")]
    [InlineData("NTLM")]
    [InlineData("Negotiate")]
    public void GetCredential_CustomCredentialsThatAreKept_AreTheKeptOnesForEveryScheme(string scheme)
    {
        GivenTheUserChose(useDefaultCredentials: false);
        var kept = new NetworkCredential("test-user", "test-password");
        _store.Read().Returns(kept);

        var credential = CreateCredentials().GetCredential(s_proxy, scheme);

        Assert.Same(kept, credential);
    }

    [Fact]
    public void GetCredential_CustomCredentialsThatAreNotKept_FallsBackToTheWindowsUser()
    {
        GivenTheUserChose(useDefaultCredentials: false);
        _store.Read().Returns((NetworkCredential?)null);
        var credentials = CreateCredentials();

        Assert.Same(CredentialCache.DefaultNetworkCredentials, credentials.GetCredential(s_proxy, "Negotiate"));
        Assert.Null(credentials.GetCredential(s_proxy, "Basic"));
    }

    // The choice and the entry of the user hold from the next challenge, with no restart.
    [Fact]
    public void GetCredential_CredentialsKeptAfterAFirstChallenge_AreGivenAtTheNextOne()
    {
        GivenTheUserChose(useDefaultCredentials: true);
        var credentials = CreateCredentials();
        var first = credentials.GetCredential(s_proxy, "Basic");

        GivenTheUserChose(useDefaultCredentials: false);
        var kept = new NetworkCredential("test-user", "test-password");
        _store.Read().Returns(kept);
        var second = credentials.GetCredential(s_proxy, "Basic");

        Assert.Null(first);
        Assert.Same(kept, second);
    }

    private ProxyCredentials CreateCredentials()
    {
        return new ProxyCredentials(_settings, _store);
    }

    private void GivenTheUserChose(bool useDefaultCredentials)
    {
        var proxy = new ProxySettings { UseDefaultCredentials = useDefaultCredentials };
        _settings.Current.Returns(UserSettings.Default with { Proxy = proxy });
    }
}
