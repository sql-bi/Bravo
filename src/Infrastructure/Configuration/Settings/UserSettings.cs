using Sqlbi.Bravo.Infrastructure;
using System.Text.Json.Serialization;

namespace Sqlbi.Infrastructure.Configuration.Settings
{
    public class UserSettings
    {
        [JsonPropertyName("telemetryEnabled")]
        //[Newtonsoft.Json.JsonProperty("telemetryEnabled")]
        public bool TelemetryEnabled { get; set; } = AppConstants.TelemetryEnabledDefault;

        //[JsonExtensionData]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        //public Dictionary<string, JsonElement> ExtensionData { get; set; } // = new();

        //[JsonPropertyName("customData")]
        //public JsonElement? CustomData { get; set; }

        // TODO: 'string CustomOptions' is a workaround, we should instead use 'JsonElement? CustomData'

        [JsonPropertyName("customOptions")]
        //[Newtonsoft.Json.JsonProperty("options")]
        public string? CustomOptions { get; set; }
    }
}
