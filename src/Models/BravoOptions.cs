using Sqlbi.Bravo.Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class BravoOptions
    {
        [JsonPropertyName("telemetryEnabled")]
        public bool TelemetryEnabled { get; set; } = AppConstants.TelemetryEnabledDefault;

        [JsonPropertyName("telemetryKey")]
        public string TelemetryKey { get; } = AppConstants.TelemetryInstrumentationKey;

        [JsonPropertyName("customOptions")]
        public JsonElement? CustomOptions { get; set; }
    }
}
