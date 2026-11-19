using System;
using Bravo.Core.Browser;
using Bravo.Core.Settings;
using Bravo.Core.Updates;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Bravo.Api.Tests;

internal static class UseCasesServiceCollectionExtensions
{
    /// <summary>
    /// Adds the use cases that the routes call, as the application adds them, with substitutes for what is
    /// outside the process: no request leaves it, no sound plays and no browser opens.
    /// </summary>
    public static IServiceCollection AddUseCasesOfTheTests(this IServiceCollection services)
    {
        services.AddSingleton(TestApi.Application);
        services.AddSingleton<InMemoryUserSettingsStore>();
        services.AddSingleton<IUserSettingsStore>(provider => provider.GetRequiredService<InMemoryUserSettingsStore>());

        services.AddSingleton(new UpdateCheckOptions
        {
            FeedAddress = new Uri("https://feed.bravo.test/currentversion.json"),
            FileVersion = new Version(1, 2, 3, 4),
            IncludesRuntime = true,
        });
        services.AddSingleton(Substitute.For<IReleaseFeed>());
        services.AddSingleton(Substitute.For<IInstallationReader>());
        services.AddSingleton(Substitute.For<ISystemBeep>());
        services.AddTransient<UpdateCheck>();

        services.AddSingleton(Substitute.For<IBrowserLauncher>());

        return services;
    }
}
