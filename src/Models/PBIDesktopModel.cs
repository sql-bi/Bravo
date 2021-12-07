using System.Net;
using System.Text.Json.Serialization;

namespace Bravo.Models
{
    public class PBIDesktopModel
    {
        /// <summary>
        /// PowerBI desktop process identifier (system PID)
        /// </summary>
        [JsonPropertyName("instanceId")]
        public int InstanceId { get; set; }

        /// <summary>
        /// PowerBI desktop MS-SSAS instance endpoint
        /// </summary>
        [JsonPropertyName("instanceEndpoint")]
        public string? InstanceEndPoint { get; set; }

        [JsonPropertyName("reportName")]
        public string? ReportName { get; set; }
    }
}
