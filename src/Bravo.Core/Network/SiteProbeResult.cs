using System;
using Bravo.Core.Errors;

namespace Bravo.Core.Network;

/// <summary>
/// What a request to a site found.
/// </summary>
/// <param name="Proxy">The proxy server that the request goes through, or none when it goes to the site.</param>
/// <param name="Failure">Why the site did not answer, or none when it answered.</param>
public sealed record SiteProbeResult(Uri? Proxy, ErrorCode? Failure);
