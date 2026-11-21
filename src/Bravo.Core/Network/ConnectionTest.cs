using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Policies;
using Bravo.Core.Updates;

namespace Bravo.Core.Network;

/// <summary>
/// Tells whether the sites that the application needs can be reached, with the proxy settings as the user keeps
/// them now: a user who changed them tries them before the restart that applies them.
/// </summary>
public sealed class ConnectionTest(
    ISiteProbe probe,
    ConnectionTestOptions options,
    UpdateCheckOptions updates,
    ApplicationPolicies policies)
{
    private readonly ISiteProbe _probe = probe;
    private readonly ConnectionTestOptions _options = options;
    private readonly UpdateCheckOptions _updates = updates;
    private readonly ApplicationPolicies _policies = policies;

    /// <summary>
    /// Asks every site at once, and returns their results in the order of the sites.
    /// </summary>
    public async Task<IReadOnlyList<SiteConnection>> RunAsync(CancellationToken cancellationToken)
    {
        var probes = GetSites().Select(site => ProbeAsync(site, cancellationToken));

        return await Task.WhenAll(probes).ConfigureAwait(false);
    }

    // The update feed is the one of the build, and no request goes to it when a policy turned the check off.
    private IEnumerable<ConnectionTestSite> GetSites()
    {
        if (_updates.FeedAddress is not { } feedAddress || _policies.UpdateCheckEnabled == false)
        {
            return _options.Sites;
        }

        return _options.Sites.Append(new ConnectionTestSite(NetworkSite.UpdateFeed, feedAddress));
    }

    private async Task<SiteConnection> ProbeAsync(ConnectionTestSite site, CancellationToken cancellationToken)
    {
        var result = await _probe.ProbeAsync(site.Address, cancellationToken).ConfigureAwait(false);

        return new SiteConnection(site.Site, site.Address, result.Proxy, result.Failure);
    }
}
