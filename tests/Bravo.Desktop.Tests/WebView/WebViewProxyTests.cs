using System;
using System.Net;
using Bravo.Core.Settings;
using Bravo.Desktop.WebView;
using Bravo.Platform.Windows.Network;
using Xunit;

namespace Bravo.Desktop.Tests.WebView;

public sealed class WebViewProxyTests
{
    private const string ProxyAddress = "proxy.bravo.test:8080";

    private static readonly Uri s_ingestion = new("https://ingestion.bravo.test/");
    private static readonly Uri s_proxy = new("http://proxy.bravo.test:8080/");

    private readonly NetworkCredential _credentials = new("test-user", "test-password");

    [Fact]
    public void BrowserArguments_ServerOfTheUser_IsTheProxyOfTheEngine()
    {
        var proxy = new WebViewProxy(Custom(), _credentials, s_ingestion);

        Assert.Equal("--proxy-server=http://proxy.bravo.test:8080", proxy.BrowserArguments);
    }

    // The bypass list of the engine does not read the regular expressions of the user.
    [Fact]
    public void BrowserArguments_ServerOfTheUserThatLeavesOutTheServerOfThePage_IsNoProxy()
    {
        var proxy = new WebViewProxy(Custom(bypassList: @".*\.bravo\.test"), _credentials, s_ingestion);

        Assert.Equal("--no-proxy-server", proxy.BrowserArguments);
    }

    [Fact]
    public void BrowserArguments_None_IsNoProxy()
    {
        var selection = ProxySelection.For(new ProxySettings { Type = ProxyType.None });

        var proxy = new WebViewProxy(selection, _credentials, s_ingestion);

        Assert.Equal("--no-proxy-server", proxy.BrowserArguments);
    }

    // The engine follows the settings of Windows by itself. A page that calls no server outside the machine
    // needs no proxy.
    [Fact]
    public void BrowserArguments_SystemOrNoServerOfThePage_AreNone()
    {
        var system = ProxySelection.For(new ProxySettings { Type = ProxyType.System });

        Assert.Null(new WebViewProxy(system, _credentials, s_ingestion).BrowserArguments);
        Assert.Null(new WebViewProxy(Custom(), _credentials, remoteAddress: null).BrowserArguments);
    }

    // Asked with the address of the proxy and the scheme of the challenge: the store of the user answers Basic
    // with the credentials that the user entered, and nothing else.
    [Theory]
    [InlineData("http://proxy.bravo.test:8080/")]
    [InlineData("http://PROXY.bravo.test:8080")]
    public void GetCredential_ProxyOfTheServerOfThePage_IsTheCredentialsForBasic(string server)
    {
        var cache = new CredentialCache { { s_proxy, "Basic", _credentials } };
        var proxy = new WebViewProxy(Custom(), cache, s_ingestion);

        Assert.Same(_credentials, proxy.GetCredential(new Uri(server)));
    }

    [Theory]
    [InlineData("http://proxy.bravo.test:3128/")]
    [InlineData("https://proxy.bravo.test:8080/")]
    [InlineData("https://ingestion.bravo.test/")]
    [InlineData("http://other.bravo.test:8080/")]
    public void GetCredential_AnyOtherServer_IsNone(string server)
    {
        var proxy = new WebViewProxy(Custom(), _credentials, s_ingestion);

        Assert.Null(proxy.GetCredential(new Uri(server)));
    }

    [Fact]
    public void GetCredential_PageThatGoesToItsServerWithoutAProxy_IsNone()
    {
        var proxy = new WebViewProxy(Custom(bypassList: @".*\.bravo\.test"), _credentials, s_ingestion);

        Assert.Null(proxy.GetCredential(s_proxy));
        Assert.Null(new WebViewProxy(Custom(), _credentials, remoteAddress: null).GetCredential(s_proxy));
    }

    private static ProxySelection Custom(string? bypassList = null)
    {
        return ProxySelection.For(new ProxySettings { Type = ProxyType.Custom, Address = ProxyAddress, BypassList = bypassList });
    }
}
