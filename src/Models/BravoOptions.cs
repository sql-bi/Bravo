using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Configuration;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
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

        public static BravoOptions CreateFromUserPreferences()
        {
            //if (UserPreferences.Current.CustomOptions is null)
            //    UserPreferences.Current.CustomOptions = JsonDocument.Parse("{}").RootElement;

            var options = new BravoOptions
            {
                TelemetryEnabled = UserPreferences.Current.TelemetryEnabled,
                CustomOptions = UserPreferences.Current.CustomOptions,
                Theme = UserPreferences.Current.Theme,
            };

            return options;
        }
    }
}
