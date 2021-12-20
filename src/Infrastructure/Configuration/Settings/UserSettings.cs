using Sqlbi.Bravo.Infrastructure;
using System.Text.Json.Serialization;

namespace Sqlbi.Infrastructure.Configuration.Settings
{
    public class UserSettings
    {
        [JsonPropertyName("telemetryEnabled")]
        public bool TelemetryEnabled { get; set; } = AppConstants.TelemetryEnabledDefault;

        [JsonPropertyName("theme")]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))] // Newtonsoft.Json attribute required by WritableOptions<T>, see Update() metod
        public ThemeType Theme { get; set; } = ThemeType.Auto;

        //[JsonExtensionData]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        //public Dictionary<string, JsonElement> ExtensionData { get; set; } // = new();

        //[JsonPropertyName("customData")]
        //public JsonElement? CustomData { get; set; }

        // TODO: 'string CustomOptions' is a workaround, we should instead use 'JsonElement? CustomData'

        [JsonPropertyName("customOptions")]
        public string? CustomOptions { get; set; }
    }

    public enum ThemeType
    {
        [JsonPropertyName("auto")]
        Auto,

        [JsonPropertyName("light")]
        Light,

        [JsonPropertyName("dark")]
        Dark
    }
}
