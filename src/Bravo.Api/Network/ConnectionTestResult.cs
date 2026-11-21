using System.Collections.Generic;

namespace Bravo.Api.Network;

// Public, as every type that travels: the validation and the descriptions of the document are generated
// for public types only.
/// <summary>
/// The result of the connection test: whether each site that the application needs can be reached, with the
/// proxy settings as they are kept now.
/// </summary>
public sealed record ConnectionTestResult
{
    /// <summary>
    /// The sites, in the order in which the user reads them. A development build has only the update feed
    /// that it was given, or none.
    /// </summary>
    public required IReadOnlyList<SiteTestResult> Sites { get; init; }
}
