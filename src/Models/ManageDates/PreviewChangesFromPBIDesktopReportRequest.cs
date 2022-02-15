namespace Sqlbi.Bravo.Models.ManageDates
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class PreviewChangesFromPBIDesktopReportRequest
    {
        [Required]
        [JsonPropertyName("report")]
        public PBIDesktopReport? Report { get; set; }

        [Required]
        [JsonPropertyName("settings")]
        public PreviewChangesSettings? Settings { get; set; }
    }
}
