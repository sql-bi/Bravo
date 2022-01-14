using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure.Messages
{
    internal interface IWebMessage
    {
        /// <summary>
        /// Web message type identifier
        /// </summary>
        [Required]
        [JsonPropertyName("type")]
        WebMessageType MessageType { get; }
    }

    internal class ApplicationUpdateAvailableWebMessage : IWebMessage
    {
        public WebMessageType MessageType => WebMessageType.ApplicationUpdateAvailable;

        [JsonPropertyName("currentVersion")]
        public string? CurrentVersion { get; set; }

        [JsonPropertyName("installedVersion")]
        public string? InstalledVersion { get; set; }

        [JsonPropertyName("downloadUrl")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("changelogUrl")]
        public string? ChangelogUrl { get; set; }
    }

    internal class NetworkStatusChangedWebMessage : IWebMessage
    {
        public WebMessageType MessageType => WebMessageType.NetworkStatusChanged;

        [JsonPropertyName("internetAccess")]
        public bool InternetAccess { get; set; }
    }

    internal class PBIDesktopReportOpenWebMessage : IWebMessage
    {
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
        public WebMessageType MessageType => WebMessageType.VpaxFileOpen;

        /// <summary>
        /// The full path of the file to open
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }
}
