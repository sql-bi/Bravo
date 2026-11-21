using System.Collections.Generic;

namespace Bravo.Core.Network;

/// <summary>
/// What the connection test asks for, beside the update feed of the build.
/// </summary>
public sealed record ConnectionTestOptions
{
    /// <summary>
    /// The sites, in the order in which the user reads them.
    /// </summary>
    public required IReadOnlyList<ConnectionTestSite> Sites { get; init; }
}
