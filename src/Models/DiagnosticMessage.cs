namespace Sqlbi.Bravo.Models
{
    using System;
    using System.Text.Json.Serialization;

    public class DiagnosticMessage
    {
        [JsonPropertyName("type")]
        public DiagnosticMessageType Type { get; set; } = DiagnosticMessageType.Text;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTimeOffset? Timestamp { get; set; }

        [JsonIgnore]
        public DateTimeOffset? LastReadTimestamp { get; set; }

        public static DiagnosticMessage Create(DiagnosticMessageType type, string name, string content)
        {
            var message = new DiagnosticMessage
            {
                Type = type,
                Name = name,
                Content = content,
                Timestamp = DateTimeOffset.UtcNow,
                LastReadTimestamp = null
            };

            return message;
        }
    }

    public enum DiagnosticMessageType
    {
        Text = 0,
        Json = 1,
    }
}
