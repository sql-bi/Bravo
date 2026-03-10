namespace Sqlbi.Bravo.Infrastructure.Services;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

/// <summary>
/// Provides the listening address used by the Kestrel HTTP server
/// </summary>
public interface IServerAddressProvider
{
    /// <summary>
    /// Gets the listening address used by the Kestrel HTTP server
    /// </summary>
    string GetListeningAddress();
}

/// <inheritdoc cref="IServerAddressProvider"/>
internal sealed class ServerAddressProvider : IServerAddressProvider
{
    private readonly IServer _server;

    public ServerAddressProvider(IServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    /// <inheritdoc/>
    public string GetListeningAddress()
    {
        var feature = _server.Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("The IServerAddressesFeature is not available.");

        if (feature.Addresses.Count != 1)
            throw new InvalidOperationException($"Expected one address, but found {feature.Addresses.Count}.");

        var address = feature.Addresses.First();

        // Ensure the address ends with a slash
        if (!address.EndsWith('/'))
            address += "/";

        return address;
    }
}