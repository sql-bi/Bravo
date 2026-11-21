using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Bravo.Core.Settings;

namespace Bravo.Platform.Windows.Network;

/// <summary>
/// The proxy that the proxy settings of the user ask for, as the connections use it: the one of the system,
/// none, or the server that the user named.
/// </summary>
public sealed class ProxySelection
{
    // How long a connection is kept before a new one is made, which asks the name of its server again.
    private static readonly TimeSpan s_connectionLifetime = TimeSpan.FromMinutes(2);

    private readonly bool _useProxy;
    private readonly IWebProxy? _proxy;

    private ProxySelection(bool useProxy, IWebProxy? proxy)
    {
        _useProxy = useProxy;
        _proxy = proxy;
    }

    public static ProxySelection For(ProxySettings settings, ProxySelectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(options);

        return settings.Type switch
        {
            // Left to .NET, with no proxy object in between: it follows the settings of Windows, and goes on
            // to the next proxy of a script when one does not answer.
            ProxyType.System => new ProxySelection(useProxy: true, options.SystemProxy),
            ProxyType.None => new ProxySelection(useProxy: false, proxy: null),
            ProxyType.Custom => ForServer(settings),
            _ => throw new ArgumentOutOfRangeException(nameof(settings), settings.Type, "No proxy for the type."),
        };
    }

    /// <summary>
    /// A handler whose requests go through the proxy, and fail with the error of their cause.
    /// </summary>
    /// <param name="credentials">What answers a proxy server that asks for credentials.</param>
    public NetworkFailureHandler CreateHandler(ICredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var handler = new SocketsHttpHandler
        {
            UseProxy = _useProxy,
            Proxy = _proxy,
            DefaultProxyCredentials = credentials,
            PooledConnectionLifetime = s_connectionLifetime,
        };

        return new NetworkFailureHandler { InnerHandler = handler };
    }

    /// <summary>
    /// The proxy server that a request to an address goes through, or <see langword="null"/> when it goes to
    /// its server. Where a script names several, it is the first one.
    /// </summary>
    public Uri? FindProxy(Uri destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!_useProxy)
        {
            return null;
        }

        var proxy = _proxy ?? HttpClient.DefaultProxy;
        if (proxy.IsBypassed(destination))
        {
            return null;
        }

        // A proxy may answer a destination that it does not serve with the destination itself.
        var address = proxy.GetProxy(destination);

        return address == destination ? null : address;
    }

    private static ProxySelection ForServer(ProxySettings settings)
    {
        if (settings.GetServerAddress() is not { } address)
        {
            return new ProxySelection(useProxy: false, proxy: null);
        }

        var proxy = new WebProxy(address, settings.BypassOnLocal, settings.GetBypassPatterns().ToArray());

        return new ProxySelection(useProxy: true, proxy);
    }
}
