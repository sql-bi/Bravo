using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bravo.Core.Settings;
using Bravo.Platform.Windows.Network;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Network;

public sealed class ProxySelectionTests
{
    private static readonly Uri s_server = new("http://proxy.bravo.test:8080/");

    private readonly NetworkCredential _credentials = new("test-user", "test-password");

    // No proxy object of the application: .NET uses its own, which follows the settings of Windows and goes on
    // to the next proxy of a script.
    [Fact]
    public void For_System_LeavesTheProxyToDotNetAndGivesItTheCredentials()
    {
        var selection = ProxySelection.For(new ProxySettings { Type = ProxyType.System });

        using var handler = selection.CreateHandler(_credentials);

        var sockets = SocketsOf(handler);
        Assert.True(sockets.UseProxy);
        Assert.Null(sockets.Proxy);
        Assert.Same(_credentials, sockets.DefaultProxyCredentials);
    }

    [Theory]
    [InlineData(ProxyType.None, "proxy.bravo.test:8080")]
    [InlineData(ProxyType.Custom, null)]
    [InlineData(ProxyType.Custom, "  ")]
    public void For_NoneOrAServerWithoutAnAddress_UsesNoProxy(ProxyType type, string? address)
    {
        var settings = new ProxySettings { Type = type, Address = address };
        var selection = ProxySelection.For(settings);

        using var handler = selection.CreateHandler(_credentials);

        Assert.False(SocketsOf(handler).UseProxy);
    }

    [Fact]
    public void For_Custom_UsesTheServerOfTheUserWithItsExclusions()
    {
        var settings = new ProxySettings
        {
            Type = ProxyType.Custom,
            Address = "proxy.bravo.test:8080",
            BypassOnLocal = true,
            BypassList = @".*\.contoso\.test; 10\..*",
        };
        var selection = ProxySelection.For(settings);

        using var handler = selection.CreateHandler(_credentials);

        var proxy = Assert.IsType<WebProxy>(SocketsOf(handler).Proxy);
        Assert.Equal((s_server, true), (proxy.Address, proxy.BypassProxyOnLocal));
        Assert.Equal([@".*\.contoso\.test", @"10\..*"], proxy.BypassList);
    }

    // The credentials are not on the proxy object: one answer serves the proxy of the system and the one of
    // the user alike.
    [Fact]
    public void For_Custom_AnswersItsServerWithTheSameCredentialsAsTheSystemProxy()
    {
        var settings = new ProxySettings { Type = ProxyType.Custom, Address = "proxy.bravo.test:8080" };
        var selection = ProxySelection.For(settings);

        using var handler = selection.CreateHandler(_credentials);

        var sockets = SocketsOf(handler);
        Assert.Null(sockets.Proxy?.Credentials);
        Assert.Same(_credentials, sockets.DefaultProxyCredentials);
    }

    // The trace of the work that sends a request is recorded in the log of the host, and stays on the machine.
    [Fact]
    public async Task CreateHandler_RequestUnderAnActivity_TellsItsServerNothingOfTheTrace()
    {
        await using var server = new TestProxy();
        var selection = ProxySelection.For(new ProxySettings { Type = ProxyType.None });
        using var client = new HttpClient(selection.CreateHandler(_credentials));
        using var activity = new Activity("Bravo.Tests").Start();

        using var response = await client.GetAsync(server.Address, TestContext.Current.CancellationToken);

        Assert.Null(Assert.Single(server.Requests).TraceParent);
    }

    [Fact]
    public void ForProcess_None_UsesNoProxy()
    {
        var selection = ProxySelection.For(new ProxySettings { Type = ProxyType.None });

        var proxy = selection.ForProcess(new WebProxy(s_server), _credentials);

        Assert.True(proxy.IsBypassed(new Uri("https://server.bravo.test/")));
    }

    // The object of .NET goes on to the next proxy of a script; one of the application would not.
    [Fact]
    public void ForProcess_System_KeepsTheProxyOfDotNetAndGivesItTheCredentials()
    {
        var selection = ProxySelection.For(new ProxySettings { Type = ProxyType.System });
        var systemProxy = new WebProxy(s_server);

        var proxy = selection.ForProcess(systemProxy, _credentials);

        Assert.Same(systemProxy, proxy);
        Assert.Same(_credentials, proxy.Credentials);
    }

    [Fact]
    public void ForProcess_SystemProxyWithCredentialsOfItsOwn_KeepsThem()
    {
        var selection = ProxySelection.For(new ProxySettings { Type = ProxyType.System });
        var own = new NetworkCredential("environment-user", "environment-password");
        var systemProxy = new WebProxy(s_server) { Credentials = own };

        var proxy = selection.ForProcess(systemProxy, _credentials);

        Assert.Same(own, proxy.Credentials);
    }

    [Fact]
    public void ForProcess_Custom_UsesTheServerOfTheUserWithItsExclusionsAndTheCredentials()
    {
        var settings = new ProxySettings
        {
            Type = ProxyType.Custom,
            Address = "proxy.bravo.test:8080",
            BypassOnLocal = true,
            BypassList = @".*\.contoso\.test",
        };
        var selection = ProxySelection.For(settings);

        var proxy = Assert.IsType<WebProxy>(selection.ForProcess(new WebProxy(), _credentials));

        Assert.Equal((s_server, true, _credentials), (proxy.Address, proxy.BypassProxyOnLocal, proxy.Credentials));
        Assert.Equal([@".*\.contoso\.test"], proxy.BypassList);
    }

    [Theory]
    [InlineData(ProxyType.System, true)]
    [InlineData(ProxyType.None, false)]
    [InlineData(ProxyType.Custom, false)]
    public void FollowsSystem_IsTrueForTheProxyOfTheSystemAlone(ProxyType type, bool expected)
    {
        var selection = ProxySelection.For(new ProxySettings { Type = type, Address = "proxy.bravo.test:8080" });

        Assert.Equal(expected, selection.FollowsSystem);
    }

    [Theory]
    [InlineData("https://ingestion.bravo.test/", "http://proxy.bravo.test:8080/")]
    [InlineData("https://ingestion.contoso.test/", null)]
    [InlineData("http://localhost:8097/", null)]
    public void GetProxy_Custom_IsTheServerOfTheUserSaveForItsExclusions(string destination, string? expected)
    {
        var settings = new ProxySettings
        {
            Type = ProxyType.Custom,
            Address = "proxy.bravo.test:8080",
            BypassOnLocal = true,
            BypassList = @".*\.contoso\.test",
        };
        var selection = ProxySelection.For(settings);

        var proxy = selection.GetProxy(new Uri(destination), new WebProxy(new Uri("http://system.bravo.test:3128/")));

        Assert.Equal(expected, proxy?.ToString());
    }

    [Fact]
    public void GetProxy_System_IsTheOneThatDotNetNamesForTheAddress()
    {
        var selection = ProxySelection.For(new ProxySettings { Type = ProxyType.System });
        var systemProxy = new WebProxy(new Uri("http://system.bravo.test:3128/"), BypassOnLocal: false, [@".*\.contoso\.test"]);

        Assert.Equal(new Uri("http://system.bravo.test:3128/"), selection.GetProxy(new Uri("https://ingestion.bravo.test/"), systemProxy));
        Assert.Null(selection.GetProxy(new Uri("https://ingestion.contoso.test/"), systemProxy));
    }

    [Fact]
    public void GetProxy_None_IsNoProxy()
    {
        var selection = ProxySelection.For(new ProxySettings { Type = ProxyType.None });

        Assert.Null(selection.GetProxy(new Uri("https://ingestion.bravo.test/"), new WebProxy(s_server)));
    }

    // The handler of the application wraps the one of .NET that holds the proxy.
    private static SocketsHttpHandler SocketsOf(NetworkFailureHandler handler)
    {
        return (SocketsHttpHandler)handler.InnerHandler!;
    }
}
