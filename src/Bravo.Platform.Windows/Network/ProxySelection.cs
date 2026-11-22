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

    public static ProxySelection For(ProxySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings.Type switch
        {
            // Left to .NET, with no proxy object in between: it follows the settings of Windows, and goes on
            // to the next proxy of a script when one does not answer.
            ProxyType.System => new ProxySelection(useProxy: true, proxy: null),
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

            // A request sent while an activity is current would tell its trace to the server, in a traceparent
            // header: the trace stays on the machine.
            ActivityHeadersPropagator = null,
        };

        return new NetworkFailureHandler { InnerHandler = handler };
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
