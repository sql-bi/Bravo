namespace Sqlbi.Bravo.Models
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class BravoOptions: IUserSettings
    {
        public BravoOptions()
        {
        }

        private BravoOptions(IUserSettings userSettings)
        {
            TelemetryEnabled = userSettings.TelemetryEnabled;
            DiagnosticLevel = userSettings.DiagnosticLevel;
            UpdateChannel = userSettings.UpdateChannel;
            UpdateCheckEnabled = userSettings.UpdateCheckEnabled;
            Theme = userSettings.Theme;
            Proxy = userSettings.Proxy;
            UseSystemBrowserForAuthentication = userSettings.UseSystemBrowserForAuthentication;
            TemplateDevelopmentEnabled = userSettings.TemplateDevelopmentEnabled;
            CustomOptions = userSettings.CustomOptions;
        }

        [JsonPropertyName("telemetryEnabled")]
        public bool TelemetryEnabled { get; set; } = UserSettings.DefaultTelemetryEnabled;

        [JsonPropertyName("diagnosticLevel")]
        public DiagnosticLevelType DiagnosticLevel { get; set; } = UserSettings.DefaultDiagnosticLevel;

        [JsonPropertyName("updateChannel")]
        public UpdateChannelType UpdateChannel { get; set; } = UserSettings.DefaultUpdateChannel;

        [JsonPropertyName("updateCheckEnabled")]
        public bool UpdateCheckEnabled { get; set; } = UserSettings.DefaultUpdateCheckEnabled;

        [JsonPropertyName("theme")]
        public ThemeType Theme { get; set; } = UserSettings.DefaultTheme;

        [JsonPropertyName("proxy")]
        public ProxySettings? Proxy { get; set; }

        [JsonPropertyName("useSystemBrowserForAuthentication")]
        public bool UseSystemBrowserForAuthentication { get; set; } = UserSettings.DefaultUseSystemBrowserForAuthentication;

        [JsonPropertyName("templateDevelopmentEnabled")]
        public bool TemplateDevelopmentEnabled { get; set; } = true;

        [JsonPropertyName("manageDatesPackageRepository")]
        public string? ManageDatesPackageRepository { get; set; }

        [JsonPropertyName("customOptions")]
        public JsonElement? CustomOptions { get; set; }

        public static BravoOptions CreateFromUserPreferences()
        {
            var options = new BravoOptions(UserPreferences.Current);
            return options;
        }

        public void SaveToUserPreferences()
        {
            Validate();

            var settings = UserPreferences.Current;
            {
                settings.TelemetryEnabled = TelemetryEnabled;
                settings.DiagnosticLevel = DiagnosticLevel;
                settings.UpdateChannel = UpdateChannel;
                settings.UpdateCheckEnabled = UpdateCheckEnabled;
                settings.Theme = Theme;
                settings.Proxy = Proxy;
                settings.UseSystemBrowserForAuthentication = UseSystemBrowserForAuthentication;
                settings.TemplateDevelopmentEnabled = TemplateDevelopmentEnabled;
                settings.CustomOptions = CustomOptions;
            }
            UserPreferences.Save();
        }

        private bool Validate(bool throwOnError = true)
        {
            try
            {
                Proxy?.Validate(throwOnError);

                return true;
            }
            catch
            {
                if (throwOnError)
                    throw;

                return false;
            }
        }
    }
}
