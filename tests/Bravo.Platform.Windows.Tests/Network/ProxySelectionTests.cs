using System;
using System.Net;
using System.Net.Http;
using Bravo.Core.Settings;
using Bravo.Platform.Windows.Network;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Network;

public sealed class ProxySelectionTests
{
    private static readonly Uri s_destination = new("https://service.bravo.test/models");
    private static readonly Uri s_server = new("http://proxy.bravo.test:8080/");

    private readonly NetworkCredential _credentials = new("test-user", "test-password");

    // No proxy object of the application: .NET uses its own, which follows the settings of Windows and goes on
    // to the next proxy of a script.
    [Fact]
    public void For_System_LeavesTheProxyToDotNetAndGivesItTheCredentials()
    {
        var selection = ProxySelection.For(new ProxySettings { Type = ProxyType.System }, new ProxySelectionOptions());

        using var handler = selection.CreateHandler(_credentials);

        var sockets = Assert.IsType<SocketsHttpHandler>(handler.InnerHandler);
        Assert.True(sockets.UseProxy);
        Assert.Null(sockets.Proxy);
        Assert.Same(_credentials, sockets.DefaultProxyCredentials);
    }

    [Fact]
    public void For_SystemWithAProxyThatStandsForIt_UsesThatProxy()
    {
        var system = new WebProxy(s_server);
        var options = new ProxySelectionOptions { SystemProxy = system };
        var selection = ProxySelection.For(new ProxySettings { Type = ProxyType.System }, options);

        using var handler = selection.CreateHandler(_credentials);

        var sockets = Assert.IsType<SocketsHttpHandler>(handler.InnerHandler);
        Assert.Same(system, sockets.Proxy);
        Assert.Equal(s_server, selection.FindProxy(s_destination));
    }

    [Theory]
    [InlineData(ProxyType.None, "proxy.bravo.test:8080")]
    [InlineData(ProxyType.Custom, null)]
    [InlineData(ProxyType.Custom, "  ")]
    public void For_NoneOrAServerWithoutAnAddress_UsesNoProxy(ProxyType type, string? address)
    {
        var settings = new ProxySettings { Type = type, Address = address };
        var selection = ProxySelection.For(settings, new ProxySelectionOptions { SystemProxy = new WebProxy(s_server) });

        using var handler = selection.CreateHandler(_credentials);

        var sockets = Assert.IsType<SocketsHttpHandler>(handler.InnerHandler);
        Assert.False(sockets.UseProxy);
        Assert.Null(selection.FindProxy(s_destination));
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
        var selection = ProxySelection.For(settings, new ProxySelectionOptions());

        using var handler = selection.CreateHandler(_credentials);

        var sockets = Assert.IsType<SocketsHttpHandler>(handler.InnerHandler);
        var proxy = Assert.IsType<WebProxy>(sockets.Proxy);
        Assert.Equal(s_server, proxy.Address);
        Assert.True(proxy.BypassProxyOnLocal);
        Assert.Equal([@".*\.contoso\.test", @"10\..*"], proxy.BypassList);
    }

    // The credentials are not on the proxy object: one answer serves the proxy of the system and the one of
    // the user alike.
    [Fact]
    public void For_Custom_AnswersItsServerWithTheSameCredentialsAsTheSystemProxy()
    {
        var settings = new ProxySettings { Type = ProxyType.Custom, Address = "proxy.bravo.test:8080" };
        var selection = ProxySelection.For(settings, new ProxySelectionOptions());

        using var handler = selection.CreateHandler(_credentials);

        var sockets = Assert.IsType<SocketsHttpHandler>(handler.InnerHandler);
        Assert.Null(sockets.Proxy?.Credentials);
        Assert.Same(_credentials, sockets.DefaultProxyCredentials);
    }

    [Theory]
    [InlineData("https://service.bravo.test/models", "http://proxy.bravo.test:8080/")]
    [InlineData("https://reports.contoso.test/", null)]
    [InlineData("http://intranet/", null)]
    public void FindProxy_Custom_IsTheServerUnlessTheAddressIsExcluded(string destination, string? expected)
    {
        var settings = new ProxySettings
        {
            Type = ProxyType.Custom,
            Address = "proxy.bravo.test:8080",
            BypassOnLocal = true,
            BypassList = @".*\.contoso\.test",
        };
        var selection = ProxySelection.For(settings, new ProxySelectionOptions());

        var proxy = selection.FindProxy(new Uri(destination));

        Assert.Equal(expected is null ? null : new Uri(expected), proxy);
    }
}
