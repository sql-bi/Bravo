using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure.Messages
{
    internal enum WebMessageType
    {
        [JsonPropertyName("None")]
        None = 0,

        [JsonPropertyName("ApplicationUpdate")]
        ApplicationUpdateAvailable = 1,

        [JsonPropertyName("NetworkStatus")]
        NetworkStatusChanged = 2,

        [JsonPropertyName("ReportOpen")]
        PBIDesktopReportOpen = 3,

        [JsonPropertyName("DatasetOpen")]
        PBICloudDatasetOpen = 4,

        [JsonPropertyName("VpaxOpen")]
        VpaxFileOpen = 5,
    }
}
