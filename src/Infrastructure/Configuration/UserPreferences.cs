namespace Sqlbi.Bravo.Infrastructure.Configuration
{
    using Microsoft.Win32;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text.Json;

    internal static class UserPreferences
    {
        private static readonly JsonSerializerOptions _serializationOptions;
        private static readonly Lazy<UserSettings> _settings;

        static UserPreferences()
        {
            _settings = new Lazy<UserSettings>(CreateInstance, isThreadSafe: true);
            _serializationOptions = new()
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                WriteIndented = true,
            };
        }

        public static UserSettings Current => _settings.Value;

        public static void Save()
        {
            try
            {
                var settingsString = JsonSerializer.Serialize(Current, _serializationOptions);
                File.WriteAllText(AppEnvironment.UserSettingsFilePath, settingsString);
            }
            catch (Exception ex)
            {
                throw new BravoException(BravoProblem.UserSettingsSaveError, ex.Message, ex);
            }
        }

        private static UserSettings CreateInstance()
        {
            try
            {
                var settings = CreateInstanceFromFile();
                if (settings is not null)
                    return settings;
            }
            catch (Exception ex)
            {
                ExceptionHelper.WriteToEventLog(ex, EventLogEntryType.Warning, throwOnError: false);
            }

            // Creation from file failed, the file is corrupted, does not exists or it's empty.
            var defaultSettings = new UserSettings();
            {
                try
                {
                    UpdateFromRegistry(defaultSettings);
                }
                catch (Exception ex)
                {
                    ExceptionHelper.WriteToEventLog(ex, EventLogEntryType.Warning, throwOnError: false);
                }
            }
            return defaultSettings;
        }

        private static UserSettings? CreateInstanceFromFile()
        {
            if (File.Exists(AppEnvironment.UserSettingsFilePath))
            {
                var settingsString = File.ReadAllText(AppEnvironment.UserSettingsFilePath);
                var settings = JsonSerializer.Deserialize<UserSettings>(settingsString, _serializationOptions);

                // Input validation and sanitization
                if (settings is not null)
                {
                    // JsonSerializer does not enforce enum values
                    if (!Enum.IsDefined(typeof(ThemeType), (int)settings.Theme))
                    {
                        settings.Theme = ThemeType.Auto;
                    }
                }

                return settings;
            }

            return null;
        }

        private static void UpdateFromRegistry(UserSettings settings)
        {
            var registryKey = AppEnvironment.ApplicationInstallerRegistryHKey;
            if (registryKey is null)
                return;

            using var registrySubKey = registryKey.OpenSubKey(AppEnvironment.ApplicationRegistryKeyName, writable: false);

            if (registrySubKey is not null)
            {
                var value = registrySubKey.GetValue(AppEnvironment.ApplicationRegistryApplicationTelemetryEnableValue, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (value is not null)
                {
                    var valueKind = registrySubKey.GetValueKind(AppEnvironment.ApplicationRegistryApplicationTelemetryEnableValue);
                    if (valueKind == RegistryValueKind.String)
                    {
                        var valueString = (string)value;

                        if (int.TryParse(valueString, out var intValue))
                        {
                            settings.TelemetryEnabled = Convert.ToBoolean(intValue);
                        }
                    }
                }
            }
        }
    }
}
