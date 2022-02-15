namespace Sqlbi.Bravo.Models.ExportData
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

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
