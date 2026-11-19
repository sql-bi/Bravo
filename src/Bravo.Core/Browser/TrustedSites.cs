using System;
using System.Collections.Immutable;
using System.Linq;

namespace Bravo.Core.Browser;

/// <summary>
/// The sites whose pages the application opens in the browser: its own, the ones of its documentation and of
/// its releases, and the ones that its features link to. Any other address may come from content that a
/// model or a file brought into the page.
/// </summary>
public static class TrustedSites
{
    // A site is its host and every host below it: sqlbi.com is also docs.sqlbi.com.
    private static readonly ImmutableArray<string> s_hosts =
    [
        "bravo.bi",
        "sqlbi.com",
        "github.com",
        "microsoft.com",
        "daxformatter.com",
        "bravorelease.blob.core.windows.net",
        "code.visualstudio.com",
        "marketplace.visualstudio.com",
    ];

    /// <summary>
    /// Whether an address is the one of a web page on a trusted site: absolute, with the scheme
    /// <c>https</c> or <c>http</c>, on a host of the list or below one.
    /// </summary>
    public static bool Contains(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (!address.IsAbsoluteUri || address.IsLoopback)
        {
            return false;
        }

        if (address.Scheme != Uri.UriSchemeHttps && address.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        return s_hosts.Any(host => IsHostOrBelow(address.Host, host));
    }

    private static bool IsHostOrBelow(string candidate, string host)
    {
        return candidate.Equals(host, StringComparison.OrdinalIgnoreCase)
            || candidate.EndsWith($".{host}", StringComparison.OrdinalIgnoreCase);
    }
}
