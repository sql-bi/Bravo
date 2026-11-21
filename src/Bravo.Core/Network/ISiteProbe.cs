using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bravo.Core.Network;

/// <summary>
/// Asks a site for an answer, through the proxy of the settings of the user as they are kept now.
/// </summary>
public interface ISiteProbe
{
    /// <summary>
    /// Sends one request to the address. Any answer of the site counts, whatever its status; a site that does
    /// not answer is a result with its cause, never an exception.
    /// </summary>
    Task<SiteProbeResult> ProbeAsync(Uri address, CancellationToken cancellationToken);
}
