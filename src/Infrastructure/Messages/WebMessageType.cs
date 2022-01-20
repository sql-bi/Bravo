using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure.Messages
{
    internal enum WebMessageType
    {
        [JsonPropertyName("Unknown")]
        Unknown = 0,

        [JsonPropertyName("ApplicationUpdate")]
        ApplicationUpdateAvailable = 1,

        [JsonPropertyName("ReportOpen")]
        PBIDesktopReportOpen = 2,

        [JsonPropertyName("DatasetOpen")]
        PBICloudDatasetOpen = 3,

        [JsonPropertyName("VpaxOpen")]
        VpaxFileOpen = 4,
    }
}
