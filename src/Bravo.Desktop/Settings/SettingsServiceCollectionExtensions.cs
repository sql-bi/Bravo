using Bravo.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Bravo.Desktop.Settings;

internal static class SettingsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the settings of the user. They start from the defaults and last as long as the process.
    /// </summary>
    public static IServiceCollection AddUserSettings(this IServiceCollection services)
    {
        services.AddSingleton<IUserSettingsStore, InMemoryUserSettingsStore>();

        return services;
    }
}
