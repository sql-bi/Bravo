using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Bravo.Api.Server;

internal sealed class ServerAddressProvider(IServer server) : IApiAddressProvider
{
    private readonly IServer _server = server;

    public Uri GetAddress()
    {
        // The server writes here the port it was given, when it starts.
        var address = _server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();
        if (address is null)
        {
            throw new InvalidOperationException("The API has no address before the host has started.");
        }

        return new Uri(address);
    }
}
