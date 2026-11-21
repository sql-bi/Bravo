using System;
using Bravo.Core.Errors;

namespace Bravo.Core.Network;

/// <summary>
/// The result of the connection test for one site.
/// </summary>
/// <param name="Site">The site.</param>
/// <param name="Address">The address that was asked for.</param>
/// <param name="Proxy">The proxy server that the request goes through, or none when it goes to the site.</param>
/// <param name="Failure">Why the site did not answer, or none when it answered.</param>
public sealed record SiteConnection(NetworkSite Site, Uri Address, Uri? Proxy, ErrorCode? Failure);
