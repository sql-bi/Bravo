using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Infrastructure.Configuration.Settings;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class BravoOptions
    {
        [JsonPropertyName("telemetryEnabled")]
        public bool TelemetryEnabled { get; set; } = AppConstants.TelemetryEnabledDefault;

        [JsonPropertyName("theme")]
        public ThemeType Theme { get; set; } = ThemeType.Auto;

        [JsonPropertyName("customOptions")]
        public JsonElement? CustomOptions { get; set; }

        public static BravoOptions CreateFrom(UserSettings settings)
        {
            JsonElement? customOptionsAsJsonElement = settings.CustomOptions is not null
               ? JsonSerializer.Deserialize<JsonElement>(settings.CustomOptions)
               : null;

            var options = new BravoOptions
            {
                TelemetryEnabled = settings.TelemetryEnabled,
                Theme = settings.Theme,
                CustomOptions = customOptionsAsJsonElement
            };

            return options;
        }
    }
}
