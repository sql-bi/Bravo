using System;
using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace Bravo.Api.Server;

public static class LoopbackServerWebHostBuilderExtensions
{
    /// <summary>
    /// Adds the server of the API: Kestrel, on plain HTTP, reachable from this machine only, on a port that
    /// the system chooses when the host starts.
    /// </summary>
    public static IWebHostBuilder UseLoopbackServer(this IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseKestrelCore().ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;

            // The address and not the name localhost, which cannot be bound to a port chosen by the system.
            options.Listen(IPAddress.Loopback, port: 0);
        });
    }
}
