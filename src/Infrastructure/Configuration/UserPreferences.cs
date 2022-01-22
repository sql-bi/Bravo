using Microsoft.Win32;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Helpers;
using System;
using System.IO;
using System.Text.Json;

namespace Sqlbi.Bravo.Infrastructure.Configuration
{
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
                File.WriteAllText(AppConstants.UserSettingsFilePath, settingsString);
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
                ExceptionHelper.WriteEventLogWarning(ex);
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
                    ExceptionHelper.WriteEventLogWarning(ex);
                }
            }
            return defaultSettings;
        }

        private static UserSettings? CreateInstanceFromFile()
        {
            if (File.Exists(AppConstants.UserSettingsFilePath))
            {
                var settingsString = File.ReadAllText(AppConstants.UserSettingsFilePath);
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
            var registryObject = Registry.GetValue(AppConstants.ApplicationRegistryHKLMKeyName, AppConstants.ApplicationRegistryHKLMApplicationTelemetryEnableValue, null);
            if (registryObject is not null)
            {
                var registryValue = Convert.ToString(registryObject);

                if (bool.TryParse(registryValue, out var boolValue))
                {
                    settings.TelemetryEnabled = boolValue;
                }
                else if (int.TryParse(registryValue, out var intValue))
                {
                    settings.TelemetryEnabled = Convert.ToBoolean(intValue);
                }
            }
        }
    }
}
