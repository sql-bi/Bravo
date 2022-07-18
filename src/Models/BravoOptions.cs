namespace Sqlbi.Bravo.Models
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Security.Policies;
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
            UpdateChannelPolicy = userSettings.UpdateChannelPolicy;
            UpdateCheckEnabled = userSettings.UpdateCheckEnabled;
            UpdateCheckEnabledPolicy = userSettings.UpdateCheckEnabledPolicy;
            Theme = userSettings.Theme;
            Proxy = userSettings.Proxy;
            UseSystemBrowserForAuthentication = userSettings.UseSystemBrowserForAuthentication;
            CustomOptions = userSettings.CustomOptions;
        }

        [JsonPropertyName("telemetryEnabled")]
        public bool TelemetryEnabled { get; set; } = AppEnvironment.TelemetryEnabledDefault;

        [JsonPropertyName("updateChannel")]
        public UpdateChannelType UpdateChannel { get; set; } = UpdateChannelType.Stable;

        [JsonPropertyName("updateChannelPolicy")]
        public PolicyStatus UpdateChannelPolicy { get; set; } = PolicyStatus.NotConfigured;

        [JsonPropertyName("updateCheckEnabled")]
        public bool UpdateCheckEnabled { get; set; } = true;

        [JsonPropertyName("updateCheckEnabledPolicy")]
        public PolicyStatus UpdateCheckEnabledPolicy { get; set; } = PolicyStatus.NotConfigured;

        [JsonPropertyName("theme")]
        public ThemeType Theme { get; set; } = ThemeType.Auto;

        [JsonPropertyName("proxy")]
        public ProxySettings? Proxy { get; set; }

        [JsonPropertyName("useSystemBrowserForAuthentication")]
        public bool UseSystemBrowserForAuthentication { get; set; } = false;

        [JsonPropertyName("manageDatesPackageRepository")]
        public string? ManageDatesPackageRepository { get; set; }

        [JsonPropertyName("diagnosticLevel")]
        public DiagnosticLevelType DiagnosticLevel { get; set; } = DiagnosticLevelType.None;

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
                BravoUnexpectedException.Assert(settings.UpdateChannelPolicy == UpdateChannelPolicy);
                BravoUnexpectedException.Assert(settings.UpdateCheckEnabledPolicy == UpdateCheckEnabledPolicy);

                settings.TelemetryEnabled = TelemetryEnabled;
                settings.DiagnosticLevel = DiagnosticLevel;
                settings.UpdateChannel = UpdateChannel;
                settings.UpdateCheckEnabled = UpdateCheckEnabled;
                settings.Theme = Theme;
                settings.Proxy = Proxy;
                settings.UseSystemBrowserForAuthentication = UseSystemBrowserForAuthentication;
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
