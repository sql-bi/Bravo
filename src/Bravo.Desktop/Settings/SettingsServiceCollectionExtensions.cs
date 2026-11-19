using System;
using System.IO;
using Bravo.Core.Settings;
using Bravo.Core.Updates;
using Bravo.Platform.Windows.Settings;
using Bravo.Platform.Windows.Updates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bravo.Desktop.Settings;

internal static class SettingsServiceCollectionExtensions
{
    // The name that 1.0.x reads and writes in the same folder.
    private const string FileName = "usersettings.json";

    /// <summary>
    /// Adds the settings of the user, kept in the file of 1.0.x in the data folder and read when the host starts.
    /// </summary>
    public static IServiceCollection AddUserSettings(this IServiceCollection services, DataFolder dataFolder)
    {
        var path = Path.Combine(dataFolder.Path, FileName);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IInstallationReader>(new RegistryInstallationReader(AppContext.BaseDirectory));
        services.AddSingleton<FileReplacer>();
        services.AddSingleton(provider => ActivatorUtilities.CreateInstance<FileUserSettingsStore>(provider, path));
        services.AddSingleton<IUserSettingsStore>(provider => provider.GetRequiredService<FileUserSettingsStore>());
        services.AddHostedService<SettingsReadFailureNotice>();

        return services;
    }
}
