using Sqlbi.Bravo.Infrastructure;

namespace Sqlbi.Infrastructure
{
    public class AppOptions
    {
        [System.Text.Json.Serialization.JsonPropertyName("telemetryEnabled")]
        //[Newtonsoft.Json.JsonProperty("telemetryEnabled")]
        public bool TelemetryEnabled { get; set; } = AppConstants.TelemetryEnabledDefault;

        //[JsonExtensionData]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        //public Dictionary<string, JsonElement> ExtensionData { get; set; } // = new();

        //[JsonPropertyName("customData")]
        //public JsonElement? CustomData { get; set; }

        // TODO: 'string CustomOptions' is a workaround, we should instead use 'JsonElement? CustomData'

        [System.Text.Json.Serialization.JsonPropertyName("customOptions")]
        //[Newtonsoft.Json.JsonProperty("options")]
        public string? CustomOptions { get; set; }
    }
}
