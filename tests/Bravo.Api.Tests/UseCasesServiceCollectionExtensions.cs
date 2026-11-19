using Bravo.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Bravo.Api.Tests;

internal static class UseCasesServiceCollectionExtensions
{
    /// <summary>
    /// Adds the use cases that the routes call, as the application adds them.
    /// </summary>
    public static IServiceCollection AddUseCasesOfTheTests(this IServiceCollection services)
    {
        services.AddSingleton(TestApi.Application);
        services.AddSingleton<IUserSettingsStore, InMemoryUserSettingsStore>();

        return services;
    }
}
