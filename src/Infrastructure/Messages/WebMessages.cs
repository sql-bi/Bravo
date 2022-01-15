using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure.Messages
{
    internal interface IWebMessage
    {
        /// <summary>
        /// Web message type identifier
        /// </summary>
        WebMessageType MessageType { get; }
    }

    internal class ApplicationUpdateAvailableWebMessage : IWebMessage
    {
        [Required]
        [JsonPropertyName("type")]
        public WebMessageType MessageType => WebMessageType.ApplicationUpdateAvailable;

        [JsonPropertyName("currentVersion")]
        public string? CurrentVersion { get; set; }

        [JsonPropertyName("installedVersion")]
        public string? InstalledVersion { get; set; }

        [JsonPropertyName("downloadUrl")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("changelogUrl")]
        public string? ChangelogUrl { get; set; }

        [JsonIgnore]
        public string? AsString => JsonSerializer.Serialize(this, AppConstants.DefaultJsonOptions);
    }

    internal class NetworkStatusChangedWebMessage : IWebMessage
    {
        [Required]
        [JsonPropertyName("type")]
        public WebMessageType MessageType => WebMessageType.NetworkStatusChanged;

        [JsonPropertyName("internetAccess")]
        public bool InternetAccess { get; set; }
    }

    internal class PBIDesktopReportOpenWebMessage : IWebMessage
    {
        [Required]
        [JsonPropertyName("type")]
        public WebMessageType MessageType => WebMessageType.PBIDesktopReportOpen;

        /// <summary>
        /// PBIDesktop process identifier (system PID)
        /// </summary>
        [JsonPropertyName("id")]
        public int? ProcessId { get; set; }

        /// <summary>
        /// PBIDesktop report name (from main window title)
        /// </summary>
        [JsonPropertyName("reportName")]
        public string? ReportName { get; set; }

        /// <summary>
        /// Server name of the local instance of Analysis Services Tabular
        /// </summary>
        [JsonPropertyName("serverName")]
        public string? ServerName { get; set; }

        /// <summary>
        /// Database name of the model hosted in the local instance of Analysis Services Tabular
        /// </summary>
        [JsonPropertyName("databaseName")]
        public string? DatabaseName { get; set; }
    }

    internal class PBICloudDatasetOpenWebMessage : IWebMessage
    {
        [Required]
        [JsonPropertyName("type")]
        public WebMessageType MessageType => WebMessageType.PBICloudDatasetOpen;

        /// <summary>
        /// PBICloud dataset server name - i.e. "pbiazure://api.powerbi.com/"
        /// </summary>
        [JsonPropertyName("serverName")]
        public string? ServerName { get; set; }

        /// <summary>
        /// PBICloud dataset database name
        /// </summary>
        [JsonPropertyName("databaseName")]
        public string? DatabaseName { get; set; }
    }

    internal class VpaxFileOpenWebMessage : IWebMessage
    {
        [Required]
        [JsonPropertyName("type")]
        public WebMessageType MessageType => WebMessageType.VpaxFileOpen;

        /// <summary>
        /// The full path of the file to open
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }
}
