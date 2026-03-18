namespace Sqlbi.Bravo.Infrastructure.Services;

using Microsoft.Web.WebView2.Core;
using Sqlbi.Bravo.Infrastructure.Configuration;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Security;
using Sqlbi.Bravo.Infrastructure.Telemetry;

/// <summary>
/// Handles WebView2 proxy authentication challenges (HTTP 407).
///
/// The WebView2-hosted UI communicates only with the local Kestrel server (localhost),
/// which bypasses the proxy. The only external HTTP traffic originating from the WebView2
/// is the Application Insights telemetry sent by the TypeScript UI layer directly to the
/// ingestion endpoint over the internet.
///
/// Therefore, proxy authentication challenges in this context can only be triggered by
/// telemetry requests going through the configured proxy.
///
/// Security: before providing credentials, we verify that the authentication challenge
/// comes from the user's configured proxy — not from an arbitrary or rogue server. This
/// prevents credential leakage to untrusted endpoints (e.g. a server returning a 401 to
/// harvest proxy credentials).
/// </summary>
internal sealed class WebView2ProxyAuthHandler
{
    private readonly IWebProxy _webProxy;

    public WebView2ProxyAuthHandler(IWebProxy webProxy)
    {
        _webProxy = webProxy;
    }

    /// <summary>
    /// Attempts to handle a proxy authentication challenge.
    /// Returns true if credentials were provided, false otherwise.
    /// </summary>
    public bool TryHandle(CoreWebView2BasicAuthenticationRequestedEventArgs e)
    {
        var proxy = UserPreferences.Current.Proxy;

        // No proxy configured
        if (proxy is null)
            return false;

        // Proxy explicitly disabled
        if (proxy.Type == ProxyType.None)
            return false;

        // Proxy configured to use system credentials, we can't handle the challenge
        if (proxy.UseDefaultCredentials)
            return false;

        // Verify the auth challenge comes from the configured proxy, not from an arbitrary server.
        if (!IsTrustedProxy(e.Uri))
            return false;

        if (CredentialManager.TryGetCredential(targetName: AppEnvironment.CredentialManagerProxyCredentialName, out var genericCredential))
        {
            var credential = genericCredential.ToNetworkCredential();
            if (credential is not null)
            {
                e.Response.UserName = credential.UserName;
                e.Response.Password = credential.Password;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Verifies that the URI from the authentication challenge matches the proxy that
    /// the system would use for the telemetry ingestion endpoint.
    ///
    /// This works for both Custom and System proxy types: <see cref="IWebProxy.GetProxy"/>
    /// resolves the correct proxy URI in both cases (including PAC/WPAD auto-discovery
    /// for system proxies).
    ///
    /// We compare only scheme, host, and port — the path component is irrelevant for
    /// proxy identity verification.
    ///
    /// Known limitation: the comparison is string-based and does not perform DNS resolution.
    /// If one URI uses a hostname (e.g. "proxy.contoso.com") and the other uses its IP address
    /// (e.g. "10.0.0.1"), the match will fail even though they refer to the same host.
    /// In practice this is unlikely because Chromium uses the same proxy URI that was passed
    /// via --proxy-server, and GetProxy() for Custom type returns the same user-configured address.
    /// For System proxies with PAC files the risk is slightly higher but remains an edge case.
    /// </summary>
    private bool IsTrustedProxy(string requestUri)
    {
        var expectedProxyUri = _webProxy.GetProxy(TelemetrySessionInfo.DefaultIngestionEndpoint);
        if (expectedProxyUri is null)
            return false;

        var requestedUri = new Uri(requestUri);

        return Uri.Compare(
            requestedUri,
            expectedProxyUri,
            UriComponents.Scheme | UriComponents.HostAndPort,
            UriFormat.Unescaped,
            StringComparison.OrdinalIgnoreCase) == 0;
    }
}
