namespace Sqlbi.Bravo.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public abstract class ExportExcelRequest
    {
        [Required]
        [JsonPropertyName("settings")]
        public ExportExcelSettings? Settings { get; set; }
    }

    public class ExportExcelFromPBIReportRequest : ExportExcelRequest
    {
        [Required]
        [JsonPropertyName("report")]
        public PBIDesktopReport? Report { get; set; }
    }

    public class ExportExcelFromPBICloudDatasetRequest : ExportExcelRequest
    {
        [Required]
        [JsonPropertyName("dataset")]
        public PBICloudDataset? Dataset { get; set; }
    }
}
