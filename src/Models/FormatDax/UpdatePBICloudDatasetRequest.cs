namespace Sqlbi.Bravo.Models.FormatDax
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class UpdatePBICloudDatasetRequest
    {
        [Required]
        [JsonPropertyName("dataset")]
        public PBICloudDataset? Dataset { get; set; }

        [Required]
        [JsonPropertyName("measures")]
        public IEnumerable<FormattedMeasure>? Measures { get; set; }
    }
}
