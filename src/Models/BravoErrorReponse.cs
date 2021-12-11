using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class BravoErrorReponse
    {
        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }

        /// <summary>
        /// The request was canceled because the configured timeout period elapsed prior to completion of the operation
        /// </summary>
        [JsonPropertyName("isTimeout")]
        public bool? IsTimeoutElapsed { get; set; }
    }
}
