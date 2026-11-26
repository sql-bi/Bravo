using System;
using System.IO;
using Bravo.Core.Settings;
using Bravo.Platform.Windows.Settings;
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
        var path = GetFilePath(dataFolder);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<FileReplacer>();
        services.AddSingleton(provider => ActivatorUtilities.CreateInstance<FileUserSettingsStore>(provider, path));
        services.AddSingleton<IUserSettingsStore>(provider => provider.GetRequiredService<FileUserSettingsStore>());
        services.AddHostedService<SettingsReadFailureNotice>();

        return services;
    }

    /// <summary>
    /// The file of the settings of a data folder.
    /// </summary>
    public static string GetFilePath(DataFolder dataFolder)
    {
        ArgumentNullException.ThrowIfNull(dataFolder);

        return Path.Combine(dataFolder.Path, FileName);
    }
}
