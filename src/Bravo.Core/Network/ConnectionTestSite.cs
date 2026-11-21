using System;

namespace Bravo.Core.Network;

/// <summary>
/// A site of the connection test, with the address that the test asks for.
/// </summary>
public sealed record ConnectionTestSite(NetworkSite Site, Uri Address);
