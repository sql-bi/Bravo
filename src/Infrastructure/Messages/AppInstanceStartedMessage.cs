using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure.Messages
{
    internal class AppInstanceStartedMessage
    {
        [JsonPropertyName("externalTool")]
        public bool IsExternalTool => AppConstants.PBIDesktopProcessName.Equals(ParentProcessName);

        [JsonPropertyName("parentProcessId")]
        public int? ParentProcessId { get; set; }

        [JsonPropertyName("parentProcessName")]
        public string? ParentProcessName { get; set; }

        [JsonPropertyName("parentProcessMainWindowTitle")]
        public string? ParentProcessMainWindowTitle { get; set; }

        [JsonPropertyName("serverName")]
        public string? ArgumentServerName { get; set; }

        [JsonPropertyName("databaseName")]
        public string? ArgumentDatabaseName { get; set; }
    }
}
