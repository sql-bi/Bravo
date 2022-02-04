namespace Sqlbi.Bravo.Models
{
    using System.Text.Json.Serialization;

    public class ExportResult
    {
        /// <summary>
        /// True if the user cancel the action, otherwise false
        /// </summary>
        [JsonPropertyName("canceled")]
        public bool? Canceled { get; set; }

        /// <summary>
        /// Full path of the selected file/folder
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }
}
