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

        [Required]
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        //[JsonPropertyName("displayFolder")]
        //public string? DisplayFolder
        //{
        //    get
        //    {
        //        if (Path is not null)
        //        {
        //            var folder = System.IO.Path.GetDirectoryName(Path);
        //            if (folder is not null)
        //            {
        //                var displayFolder = CommonHelper.GetFileRelativePath(folder, Path);
        //                return displayFolder;
        //            }
        //        }

        //        return null;
        //    }
        //}

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("workspacePath")]
        public string? WorkspacePath { get; set; }

        [JsonPropertyName("workspaceName")]
        public string? WorkspaceName { get; set; }

        [JsonPropertyName("hasWorkspace")]
        public bool HasWorkspace => WorkspacePath is not null && WorkspaceName is not null;
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
