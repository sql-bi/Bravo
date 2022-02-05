namespace Sqlbi.Bravo.Models.FormatDax
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class UpdatePBIDesktopReportRequest
    {
        [Required]
        [JsonPropertyName("report")]
        public PBIDesktopReport? Report { get; set; }

        [Required]
        [JsonPropertyName("measures")]
        public IEnumerable<FormattedMeasure>? Measures { get; set; }
    }
}
