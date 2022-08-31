namespace Sqlbi.Bravo.Models.ManageDates
{
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.Text.Json.Serialization;

    public class CustomPackage
    {
        [Required]
        [JsonPropertyName("type")]
        public CustomPackageType? Type { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("workspacePath")]
        public string? WorkspacePath { get; set; }

        [JsonPropertyName("workspaceName")]
        public string? WorkspaceName { get; set; }

        [JsonPropertyName("hasWorkspace")]
        public bool HasWorkspace { get; set; }

        [JsonPropertyName("hasPackage")]
        public bool HasPackage { get; set; }
    }

    public enum CustomPackageType
    {
        /// <summary>
        /// Custom template package from the user's local repository
        /// </summary>
        User = 0,

        /// <summary>
        /// Custom template package from the organization's shared repository
        /// </summary>
        Organization = 1,
    }
}
