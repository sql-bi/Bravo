using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure.Messages
{
    internal class AppInstanceStartedMessage
    {
        [JsonPropertyName("n")]
        public string? ConnectionName { get; set; }

        [JsonPropertyName("d")]
        public string? DatabaseName { get; set; }

        [JsonPropertyName("s")]
        public string? ServerName { get; set; }
    }
}
