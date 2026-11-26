using System;
using System.Net;
using Bravo.Platform.Windows.Network;

namespace Bravo.Desktop.WebView;

/// <summary>
/// How the page reaches the one server outside the machine that it calls, the endpoint of its telemetry: through
/// the proxy of the settings, as every connection of the host. It knows no type of the engine.
/// </summary>
internal sealed class WebViewProxy
{
    private const string BasicScheme = "Basic";

    private readonly ProxySelection _selection;
    private readonly ICredentials _credentials;
    private readonly Uri? _remoteAddress;

    /// <param name="selection">The proxy of the settings, read when the host started.</param>
    /// <param name="credentials">What answers a proxy server that asks for credentials.</param>
    /// <param name="remoteAddress">The server outside the machine that the page calls, or null for none.</param>
    public WebViewProxy(ProxySelection selection, ICredentials credentials, Uri? remoteAddress)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(credentials);

        _selection = selection;
        _credentials = credentials;
        _remoteAddress = remoteAddress;
        BrowserArguments = GetBrowserArguments(selection, remoteAddress);
    }

    /// <summary>
    /// The browser arguments that take the page to its server through the proxy, or null for none: with the
    /// proxy of the system the engine follows the settings of Windows by itself, and a page that calls no server
    /// outside the machine needs no proxy. The engine never sends the loopback address to a proxy.
    /// </summary>
    public string? BrowserArguments { get; }

    /// <summary>
    /// The credentials for a server that asked the webview for them with Basic: the proxy of the server of the
    /// page, with the credentials that the user entered. Null for any other server, and when the user entered
    /// none. With the proxy of the system it may run the script of the system: never on the thread of a window.
    /// </summary>
    /// <param name="server">The address of the server that asked, as the engine tells it.</param>
    public NetworkCredential? GetCredential(Uri server)
    {
        ArgumentNullException.ThrowIfNull(server);

        if (_remoteAddress is null || _selection.GetProxy(_remoteAddress) is not { } proxy || !IsSameServer(proxy, server))
        {
            return null;
        }

        return _credentials.GetCredential(proxy, BasicScheme);
    }

    // A server that the user's exclusions leave out of the proxy is reached with no proxy at all: the bypass
    // list of the engine has no regular expressions, and the page calls one server.
    private static string? GetBrowserArguments(ProxySelection selection, Uri? remoteAddress)
    {
        if (remoteAddress is null || selection.FollowsSystem)
        {
            return null;
        }

        // The scheme, the host and the port: never a user name.
        return selection.GetProxy(remoteAddress) is { } proxy
            ? $"--proxy-server={proxy.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped)}"
            : "--no-proxy-server";
    }

    private static bool IsSameServer(Uri proxy, Uri server)
    {
        return Uri.Compare(proxy, server, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;
    }
}
