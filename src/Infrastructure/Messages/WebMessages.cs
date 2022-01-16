using Sqlbi.Bravo.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure.Messages
{
    internal interface IWebMessage
    {
        /// <summary>
        /// Message type identifier
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

        [JsonPropertyName("report")]
        public PBIDesktopReport? Report { get; set; }

        public static PBIDesktopReportOpenWebMessage CreateFrom(AppInstanceStartupMessage message)
        {
            var webMessage = new PBIDesktopReportOpenWebMessage
            {
                Report = new PBIDesktopReport 
                {
                    ProcessId = message.ParentProcessId,
                    ReportName = message.ParentProcessMainWindowTitle,
                    ServerName = message.ArgumentServerName,
                    DatabaseName = message.ArgumentDatabaseName
                },
            };
            return webMessage;
        }
    }

    internal class PBICloudDatasetOpenWebMessage : IWebMessage
    {
        [Required]
        [JsonPropertyName("type")]
        public WebMessageType MessageType => WebMessageType.PBICloudDatasetOpen;

        [JsonPropertyName("dataset")]
        public PBICloudDataset? Dataset { get; set; }

        public static PBICloudDatasetOpenWebMessage CreateFrom(PBICloudDataset dataset)
        {
            var webMessage = new PBICloudDatasetOpenWebMessage
            {
                Dataset = dataset,
            };
            return webMessage;
        }
    }

    internal class VpaxFileOpenWebMessage : IWebMessage
    {
        [Required]
        [JsonPropertyName("type")]
        public WebMessageType MessageType => WebMessageType.VpaxFileOpen;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("blob")]
        public byte[]? Content { get; set; }

        [JsonPropertyName("lastModified")]
        public long? LastModified { get; set; }
    }
}
