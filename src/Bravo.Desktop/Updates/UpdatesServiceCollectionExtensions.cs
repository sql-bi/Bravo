using System;
using Bravo.Core.Updates;
using Bravo.Platform.Windows.Updates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bravo.Desktop.Updates;

internal static class UpdatesServiceCollectionExtensions
{
    // The feed that every installed copy reads, of whatever version: its address cannot change.
    private static readonly Uri s_releaseFeedAddress =
        new("https://bravorelease.blob.core.windows.net/bravo-public/currentversion.json");

    /// <summary>
    /// Adds the update check. A release build reads the feed of the releases; a development build reads only
    /// the one that its command line names, and without one it does not check.
    /// </summary>
    /// <param name="services">The services of the host.</param>
    /// <param name="identity">The build that was started.</param>
    /// <param name="developmentFeedAddress">The feed that the command line of a development build named.</param>
    public static IServiceCollection AddUpdates(
        this IServiceCollection services,
        BuildIdentity identity,
        Uri? developmentFeedAddress)
    {
        var options = new UpdateCheckOptions
        {
            FeedAddress = identity.IsDevelopmentBuild ? developmentFeedAddress : s_releaseFeedAddress,
            FileVersion = identity.FileVersion,
            IncludesRuntime = identity.IncludesRuntime,
        };

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(options);
        services.AddHttpClient<IReleaseFeed, HttpReleaseFeed>();
        services.AddSingleton<IInstallationReader>(new RegistryInstallationReader(AppContext.BaseDirectory));
        services.AddSingleton<ISystemBeep, SystemBeep>();
        services.AddTransient<UpdateCheck>();

        return services;
    }
}
