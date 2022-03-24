namespace Sqlbi.Bravo.Models.FormatDax
{
    using System.Text.Json.Serialization;

    // TODO: rename to 'UpdateResponse'
    public class DatabaseUpdateResult
    {
        /// <summary>
        /// The unique identifier of the current version of the tabular model computed after the update
        /// </summary>
        [JsonPropertyName("etag")]
        public string? DatabaseETag { get; set; }
    }
}
