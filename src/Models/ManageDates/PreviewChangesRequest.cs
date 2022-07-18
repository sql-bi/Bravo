namespace Sqlbi.Bravo.Models.ManageDates
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class PreviewChangesFromPBIDesktopReportRequest // TODO: rename to DateConfigurationPreviewChangesRequest
    {
        [Required]
        [JsonPropertyName("report")]
        public PBIDesktopReport? Report { get; set; }

        [Required]
        [JsonPropertyName("settings")]
        public PreviewChangesSettings? Settings { get; set; }
    }

    public class PreviewChangesSettings  // TODO: rename to DateConfigurationPreviewChangesSettings
    {
        /// <summary>
        /// <see cref="DateConfiguration"/> to be applied
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

    public class CustomPackagePreviewChangesRequest
    {
        [Required]
        [JsonPropertyName("report")]
        public PBIDesktopReport? Report { get; set; }

        [Required]
        [JsonPropertyName("settings")]
        public CustomPackagePreviewChangesSettings? Settings { get; set; }
    }

    public class CustomPackagePreviewChangesSettings
    {
        /// <summary>
        /// <see cref="CustomPackage"/> to be applied
        /// </summary>
        [Required]
        [JsonPropertyName("customPackage")]
        public CustomPackage? CustomPackage { get; set; }

        /// <summary>
        /// Number of records generated as a preview of requested changes
        /// </summary>
        [Required]
        [JsonPropertyName("previewRows")]
        public int PreviewRows { get; set; } = 0;
    }
}
