using System;

namespace Bravo.Api.Server;

/// <summary>
/// Where the API listens. The port is chosen by the system when the host starts, so the address is known
/// only from then on.
/// </summary>
public interface IApiAddressProvider
{
    /// <summary>
    /// The address that a client puts before the path of a route: scheme, loopback address and port.
    /// </summary>
    /// <exception cref="InvalidOperationException">The host has not started.</exception>
    Uri GetAddress();
}
