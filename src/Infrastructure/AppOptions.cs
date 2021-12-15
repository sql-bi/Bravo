using Sqlbi.Bravo.Infrastructure;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Sqlbi.Infrastructure
{
    public class AppOptions
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

    public class AppStartupOptions
    {
        [JsonPropertyName("executedAsExternalTool")]
        public bool IsExecutedAsExternalTool { get; set; }

        [JsonIgnore]
        public ReadOnlyCollection<string>? CommandLineErrors { get; set; }

        [JsonPropertyName("serverName")]
        public string? ArgumentServerName { get; set; }

        [JsonPropertyName("databaseName")]
        public string? ArgumentDatabaseName { get; set; }
    }
}
