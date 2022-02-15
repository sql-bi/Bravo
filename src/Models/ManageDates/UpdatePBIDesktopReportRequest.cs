namespace Sqlbi.Bravo.Models.ManageDates
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class UpdatePBIDesktopReportRequest
    {
        [Required]
        [JsonPropertyName("report")]
        public PBIDesktopReport? Report { get; set; }

        [Required]
        [JsonPropertyName("configuration")]
        public DateConfiguration? Configuration { get; set; }
    }
}
