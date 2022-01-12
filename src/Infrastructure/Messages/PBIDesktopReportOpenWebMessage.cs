using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure.Messages
{
    internal enum WebMessageType
    {
        None = 0,
        PBIDesktopReportOpen = 1,
        PBICloudDatasetOpen = 2,
    }

    internal class PBIDesktopReportOpenWebMessage
    {
        /// <summary>
        /// Web message type identifier
        /// </summary>
        [JsonPropertyName("type")]
        public WebMessageType MessageType = WebMessageType.PBIDesktopReportOpen;

        /// <summary>
        /// PBIDesktop process identifier (system PID)
        /// </summary>
        [Required]
        [JsonPropertyName("id")]
        public int? ProcessId { get; set; }
    }

    internal class PBICloudDatasetOpenWebMessage
    {
        /// <summary>
        /// Web message type identifier
        /// </summary>
        [JsonPropertyName("type")]
        public WebMessageType MessageType = WebMessageType.PBICloudDatasetOpen;
    }
}
