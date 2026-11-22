using System;
using Bravo.Api.Diagnostics;
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
    /// outside the process: no request leaves it, no sound plays, no browser and no dialog opens, and no
    /// credentials are kept.
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

        services.AddSingleton(Substitute.For<IProxyCredentialDialog>());
        services.AddSingleton(Substitute.For<IProxyCredentialStore>());
        services.AddTransient<ProxyCredentialsPrompt>();

        // The records of the log, which no logger of the tests writes: a test adds the ones that it reads.
        services.AddSingleton(new LogBuffer());

        return services;
    }
}
