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

        [JsonPropertyName("diagnosticEnabled")]
        public bool DiagnosticEnabled { get; set; } = false;

        [JsonPropertyName("customOptions")]
        public JsonElement? CustomOptions { get; set; }

        public static BravoOptions CreateFromUserPreferences()
        {
            var options = new BravoOptions
            {
                TelemetryEnabled = UserPreferences.Current.TelemetryEnabled,
                DiagnosticEnabled = UserPreferences.Current.DiagnosticEnabled,
                CustomOptions = UserPreferences.Current.CustomOptions,
                UpdateChannel = UserPreferences.Current.UpdateChannel,
                Theme = UserPreferences.Current.Theme,
            };

            return options;
        }

        public void SaveToUserPreferences()
        {
            UserPreferences.Current.TelemetryEnabled = TelemetryEnabled;
            UserPreferences.Current.DiagnosticEnabled = DiagnosticEnabled;
            UserPreferences.Current.CustomOptions = CustomOptions;
            UserPreferences.Current.UpdateChannel = UpdateChannel;
            UserPreferences.Current.Theme = Theme;
            UserPreferences.Save();
        }
    }
}
