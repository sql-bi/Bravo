using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class DatabaseUpdateResult
    {
        /// <summary>
        /// The unique identifier of the current version of the tabular model computed after the update
        /// </summary>
        [JsonPropertyName("etag")]
        public string? DatabaseETag { get; set; }
    }
}
