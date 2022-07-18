namespace Sqlbi.Bravo.Models.ManageDates
{
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.Text.Json.Serialization;

    [DebuggerDisplay("{Name} {IsCurrent}")]
    public class CustomPackage
    {
        [Required]
        [JsonPropertyName("type")]
        public CustomPackageType? Type { get; set; }

        [JsonPropertyName("folder")]
        public string? Folder { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [Required]
        [JsonPropertyName("path")]
        public string? Path { get; set; }
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
