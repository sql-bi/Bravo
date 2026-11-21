using System;
using Bravo.Core.Browser;
using Bravo.Core.Network;
using Bravo.Core.Policies;
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
    /// <param name="services">The services of the API.</param>
    /// <param name="policies">What an administrator set, as the application reads it at its start; none by default.</param>
    public static IServiceCollection AddUseCasesOfTheTests(this IServiceCollection services, ApplicationPolicies? policies = null)
    {
        services.AddSingleton(TestApi.Application);
        services.AddSingleton(policies ?? ApplicationPolicies.None);
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

        services.AddSingleton(new ConnectionTestOptions
        {
            Sites = [new ConnectionTestSite(NetworkSite.PowerBIService, new Uri("https://service.bravo.test/"))],
        });
        services.AddSingleton(Substitute.For<ISiteProbe>());
        services.AddTransient<ConnectionTest>();

        return services;
    }
}
