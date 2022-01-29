namespace Sqlbi.Bravo.Infrastructure.Configuration.Settings
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class UserSettings
    {
        [JsonPropertyName("telemetryEnabled")]
        public bool TelemetryEnabled { get; set; } = AppEnvironment.TelemetryEnabledDefault;

        [JsonPropertyName("theme")]
        public ThemeType Theme { get; set; } = ThemeType.Auto;

        [JsonPropertyName("customOptions")]
        public JsonElement? CustomOptions { get; set; }
    }

    public enum ThemeType
    {
        Auto = 0,

        Light = 1,

        Dark = 2
    }
}
