using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class BravoErrorReponse
    {
        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }
    }
}
