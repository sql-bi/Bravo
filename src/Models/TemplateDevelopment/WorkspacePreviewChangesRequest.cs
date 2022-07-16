namespace Sqlbi.Bravo.Models.TemplateDevelopment
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class WorkspacePreviewChangesRequest
    {
        [Required]
        [JsonPropertyName("report")]
        public PBIDesktopReport? Report { get; set; }

        [Required]
        [JsonPropertyName("settings")]
        public WorkspacePreviewChangesSettings? Settings { get; set; }
    }

    public class WorkspacePreviewChangesSettings
    {
        /// <summary>
        /// Full path of the package to be applied
        /// </summary>
        [Required]
        [JsonPropertyName("package")]
        public string? Package { get; set; }

        /// <summary>
        /// Number of records generated as a preview of requested changes
        /// </summary>
        [Required]
        [JsonPropertyName("previewRows")]
        public int PreviewRows { get; set; } = 0;
    }
}
