using System;
using System.Globalization;
using Bravo.Core.Application;
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

    /// <summary>
    /// Adds what the application is, as the API tells it: the version and the kind of its build, and the
    /// culture of the thread that starts the application, which is the culture of the user.
    /// </summary>
    public static IServiceCollection AddApplicationInfo(this IServiceCollection services, BuildIdentity identity)
    {
        var application = new ApplicationInfo
        {
            Version = identity.Version,
            Culture = CultureInfo.CurrentCulture,
            IsDevelopmentBuild = identity.IsDevelopmentBuild,
        };
        services.AddSingleton(application);

        return services;
    }
}
