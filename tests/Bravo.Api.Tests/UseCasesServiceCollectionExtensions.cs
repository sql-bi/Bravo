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
    /// The application that the tests run as: <c>1.2.3</c>, file version four numbers.
    /// </summary>
    public static Version FileVersion { get; } = new(1, 2, 3, 4);

    /// <summary>
    /// Adds the use cases that the routes call, as the application adds them, with substitutes for what is
    /// outside the process: no request leaves it, no sound plays and no browser opens.
    /// </summary>
    public static IServiceCollection AddUseCasesOfTheTests(this IServiceCollection services)
    {
        services.AddSingleton(TestApi.Application);
        services.AddSingleton<IUserSettingsStore, InMemoryUserSettingsStore>();

        services.AddSingleton(new UpdateCheckOptions
        {
            FeedAddress = new Uri("https://feed.bravo.test/currentversion.json"),
            FileVersion = FileVersion,
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
