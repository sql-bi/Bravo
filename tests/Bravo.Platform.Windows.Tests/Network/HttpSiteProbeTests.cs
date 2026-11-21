using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Network;
using Bravo.Core.Settings;
using Bravo.Platform.Windows.Network;
using NSubstitute;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Network;

/// <summary>
/// The site has a name that only the proxy of the test answers, on the loopback address.
/// </summary>
public sealed class HttpSiteProbeTests
{
    private static readonly Uri s_site = new("http://service.bravo.test/");

    private readonly IUserSettingsStore _settings = Substitute.For<IUserSettingsStore>();

    [Fact]
    public async Task ProbeAsync_SiteThatAnswersThroughTheProxyOfTheSettings_IsReachedAndNamesTheProxy()
    {
        await using var proxy = new TestProxy();
        GivenTheSettingsName(proxy.Address);

        var result = await CreateProbe().ProbeAsync(s_site, TestContext.Current.CancellationToken);

        Assert.Equal(new SiteProbeResult(proxy.Address, Failure: null), result);
        Assert.Equal([new TestProxyRequest("GET", s_site.AbsoluteUri, null)], proxy.Requests);
    }

    [Fact]
    public async Task ProbeAsync_ProxyThatAsksForCredentials_TellsThatCauseAndTheProxy()
    {
        await using var proxy = new TestProxy { Challenges = ["Basic realm=\"test\""] };
        GivenTheSettingsName(proxy.Address);

        var result = await CreateProbe().ProbeAsync(s_site, TestContext.Current.CancellationToken);

        Assert.Equal(new SiteProbeResult(proxy.Address, ErrorCode.ProxyAuthenticationRequired), result);
    }

    [Fact]
    public async Task ProbeAsync_ProxyThatDoesNotListen_IsServerUnreachable()
    {
        var closed = ClosedAddress();
        GivenTheSettingsName(closed);

        var result = await CreateProbe().ProbeAsync(s_site, TestContext.Current.CancellationToken);

        Assert.Equal(new SiteProbeResult(closed, ErrorCode.ServerUnreachable), result);
    }

    // The settings as they are kept when the test runs, whatever they were before.
    [Fact]
    public async Task ProbeAsync_SettingsChangedSinceTheProbeWasCreated_GoesThroughTheProxyOfTheNewOnes()
    {
        await using var first = new TestProxy();
        await using var second = new TestProxy();
        GivenTheSettingsName(first.Address);
        var probe = CreateProbe();
        GivenTheSettingsName(second.Address);

        var result = await probe.ProbeAsync(s_site, TestContext.Current.CancellationToken);

        Assert.Equal(second.Address, result.Proxy);
        Assert.Empty(first.Requests);
    }

    [Fact]
    public async Task ProbeAsync_NoProxyInTheSettings_GoesToTheSiteAndNamesNoProxy()
    {
        await using var site = new TestProxy();
        var noProxy = new ProxySettings { Type = ProxyType.None };
        _settings.Current.Returns(UserSettings.Default with { Proxy = noProxy });

        var result = await CreateProbe().ProbeAsync(site.Address, TestContext.Current.CancellationToken);

        Assert.Equal(new SiteProbeResult(Proxy: null, Failure: null), result);
        Assert.Equal([new TestProxyRequest("GET", "/", null)], site.Requests);
    }

    private HttpSiteProbe CreateProbe()
    {
        var credentials = new ProxyCredentials(_settings, Substitute.For<IProxyCredentialStore>());

        return new HttpSiteProbe(_settings, credentials, new ProxySelectionOptions());
    }

    private void GivenTheSettingsName(Uri proxy)
    {
        var settings = new ProxySettings { Type = ProxyType.Custom, Address = proxy.AbsoluteUri, BypassOnLocal = false };
        _settings.Current.Returns(UserSettings.Default with { Proxy = settings });
    }

    private static Uri ClosedAddress()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return new Uri($"http://127.0.0.1:{port}/");
    }
}
