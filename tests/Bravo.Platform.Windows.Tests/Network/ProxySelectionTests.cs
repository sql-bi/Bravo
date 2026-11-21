using System;
using System.Net;
using System.Net.Http;
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

    // The handler of the application wraps the one of .NET that holds the proxy.
    private static SocketsHttpHandler SocketsOf(NetworkFailureHandler handler)
    {
        return (SocketsHttpHandler)handler.InnerHandler!;
    }
}
