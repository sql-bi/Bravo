namespace Sqlbi.Bravo.Models
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class BravoOptions
    {
        [JsonPropertyName("telemetryEnabled")]
        public bool TelemetryEnabled { get; set; } = AppEnvironment.TelemetryEnabledDefault;

        [JsonPropertyName("updateChannel")]
        public UpdateChannelType UpdateChannel { get; set; } = UpdateChannelType.Stable;

        [JsonPropertyName("theme")]
        public ThemeType Theme { get; set; } = ThemeType.Auto;

        [JsonPropertyName("proxy")]
        public ProxySettings? Proxy { get; set; }

        [JsonPropertyName("diagnosticLevel")]
        public DiagnosticLevelType DiagnosticLevel { get; set; } = DiagnosticLevelType.None;

        [JsonPropertyName("customOptions")]
        public JsonElement? CustomOptions { get; set; }

        public static BravoOptions CreateFromUserPreferences()
        {
            var options = new BravoOptions
            {
                TelemetryEnabled = UserPreferences.Current.TelemetryEnabled,
                DiagnosticLevel = UserPreferences.Current.DiagnosticLevel,
                CustomOptions = UserPreferences.Current.CustomOptions,
                UpdateChannel = UserPreferences.Current.UpdateChannel,
                Theme = UserPreferences.Current.Theme,
                Proxy = UserPreferences.Current.Proxy,
            };

            return options;
        }

        public void SaveToUserPreferences()
        {
            UserPreferences.Current.TelemetryEnabled = TelemetryEnabled;
            UserPreferences.Current.DiagnosticLevel = DiagnosticLevel;
            UserPreferences.Current.CustomOptions = CustomOptions;
            UserPreferences.Current.UpdateChannel = UpdateChannel;
            UserPreferences.Current.Theme = Theme;
            UserPreferences.Current.Proxy = Proxy;
            UserPreferences.Save();
        }
    }
}
