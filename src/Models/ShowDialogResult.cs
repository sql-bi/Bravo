using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class ShowDialogResult
    {
        /// <summary>
        /// True if the user cancel closes the dialog box with action 'Cancel'
        /// </summary>
        [JsonPropertyName("canceled")]
        public bool? Canceled { get; set; }

        /// <summary>
        /// Full path of the selected file
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }
}
