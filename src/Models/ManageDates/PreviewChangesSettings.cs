namespace Sqlbi.Bravo.Models.ManageDates
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class PreviewChangesSettings
    {
        /// <summary>
        /// Date template configuration to apply
        /// </summary>
        [Required]
        [JsonPropertyName("configuration")]
        public DateConfiguration? Configuration { get; set; }

        /// <summary>
        /// Number of records generated as a preview of requested changes
        /// </summary>
        [Required]
        [JsonPropertyName("previewRows")]
        public int PreviewRows { get; set; } = 0;
    }
}
