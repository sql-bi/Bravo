using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bravo.Desktop.Hosting;

internal static class HostingServiceCollectionExtensions
{
    // The time Windows gives an application to end with its session. A user who closes the window waits no
    // longer than this for the process to end, and a start that follows waits for it.
    private static readonly TimeSpan s_shutdownTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Makes the host one of a desktop application: its lifetime is the main window, and its stop is short.
    /// </summary>
    public static IServiceCollection AddDesktopLifetime(this IServiceCollection services)
    {
        // The host uses the last lifetime registered: this one replaces the console lifetime of the builder.
        services.AddSingleton<IHostLifetime, DesktopHostLifetime>();
        services.Configure<HostOptions>(options => options.ShutdownTimeout = s_shutdownTimeout);

        return services;
    }
}
