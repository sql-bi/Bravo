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
    private readonly WebProxy? _proxy;

    private ProxySelection(bool useProxy, WebProxy? proxy)
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
    /// Whether the connections follow the proxy of the system, which the settings of Windows name.
    /// </summary>
    public bool FollowsSystem => _useProxy && _proxy is null;

    /// <summary>
    /// The proxy server that a request to an address goes through, or null when it goes to its server.
    /// With the proxy of the system it may run the script of the system: never on the thread of a window.
    /// </summary>
    public Uri? GetProxy(Uri destination)
    {
        return GetProxy(destination, HttpClient.DefaultProxy);
    }

    /// <param name="destination">The address of the request.</param>
    /// <param name="systemProxy">The default proxy that .NET found.</param>
    internal Uri? GetProxy(Uri destination, IWebProxy systemProxy)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!_useProxy)
        {
            return null;
        }

        var proxy = _proxy ?? systemProxy;

        return proxy.IsBypassed(destination) || proxy.GetProxy(destination) is not { } server || server == destination
            ? null
            : server;
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

    /// <summary>
    /// Gives the proxy to the HTTP clients that the host does not create, such as the one of a library: such a
    /// client takes the default proxy of the process at its first request, and keeps it.
    /// </summary>
    /// <param name="credentials">What answers a proxy server that asks for credentials.</param>
    public void ApplyToProcess(ICredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        HttpClient.DefaultProxy = ForProcess(HttpClient.DefaultProxy, credentials);
    }

    /// <summary>
    /// The default proxy of the process for this selection.
    /// </summary>
    /// <param name="systemProxy">The default proxy that .NET found.</param>
    /// <param name="credentials">What answers a proxy server that asks for credentials.</param>
    internal IWebProxy ForProcess(IWebProxy systemProxy, ICredentials credentials)
    {
        if (!_useProxy)
        {
            return new WebProxy();
        }

        if (_proxy is null)
        {
            // The object of .NET stays, which follows Windows and goes on to the next proxy of a script. The
            // credentials of a proxy named with them in an environment variable stay too.
            systemProxy.Credentials ??= credentials;
            return systemProxy;
        }

        return new WebProxy(_proxy.Address, _proxy.BypassProxyOnLocal, _proxy.BypassList) { Credentials = credentials };
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
