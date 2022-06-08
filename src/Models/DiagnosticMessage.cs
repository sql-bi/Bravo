namespace Sqlbi.Bravo.Models
{
    using System;
    using System.Text.Json.Serialization;

    public class DiagnosticMessage
    {
        [JsonPropertyName("type")]
        public DiagnosticMessageType Type { get; set; } = DiagnosticMessageType.Text;

        [JsonPropertyName("severity")]
        public DiagnosticMessageSeverity Severity { get; set; } = DiagnosticMessageSeverity.None;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? ReadTimestamp { get; set; }

        public static DiagnosticMessage Create(DiagnosticMessageType type, DiagnosticMessageSeverity severity, string name, string content)
        {
            var message = new DiagnosticMessage
            {
                Type = type,
                Severity = severity,
                Name = $"[HOST] { name }",
                Content = content,
                ReadTimestamp = null
            };

            return message;
        }
    }

    public enum DiagnosticMessageType
    {
        Text = 0,
        Json = 1,
    }

    public enum DiagnosticMessageSeverity
    {
        None = 0,
        Warning = 1,
        Error = 2,
    }
}
