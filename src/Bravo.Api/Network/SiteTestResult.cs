using Bravo.Core.Errors;
using Bravo.Core.Network;

namespace Bravo.Api.Network;

// Public, as every type that travels: the validation and the descriptions of the document are generated
// for public types only.
/// <summary>
/// What the connection test found for one site.
/// </summary>
public sealed record SiteTestResult
{
    public required NetworkSite Site { get; init; }

    /// <summary>
    /// The address that was asked for. Absolute, with every character that an address cannot hold escaped.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// The address of the proxy server that the request goes through. Absent when the request goes to the site
    /// with no proxy. Where a script names several proxy servers, it is the first one.
    /// </summary>
    public string? ProxyAddress { get; init; }

    /// <summary>
    /// Why the site did not answer: one of the codes of a request that does not reach its server. Absent when
    /// the site answered, with any status.
    /// </summary>
    public ErrorCode? Failure { get; init; }

    internal static SiteTestResult FromConnection(SiteConnection connection)
    {
        return new SiteTestResult
        {
            Site = connection.Site,
            Address = connection.Address.AbsoluteUri,
            ProxyAddress = connection.Proxy?.AbsoluteUri,
            Failure = connection.Failure,
        };
    }
}
