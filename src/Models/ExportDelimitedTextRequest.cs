using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class ExportDelimitedTextRequest
    {
        [Required]
        [JsonPropertyName("settings")]
        public ExportDelimitedTextSettings? Settings { get; set; }
    }

    public class ExportDelimitedTextFromPBIReportRequest : ExportDelimitedTextRequest
    {
        [Required]
        [JsonPropertyName("report")]
        public PBIDesktopReport? Report { get; set; }
    }

    public class ExportDelimitedTextFromPBICloudDatasetRequest : ExportDelimitedTextRequest
    {
        [Required]
        [JsonPropertyName("dataset")]
        public PBICloudDataset? Dataset { get; set; }
    }
}
