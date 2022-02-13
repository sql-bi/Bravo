using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class TabularTable
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("rowsCount")]
        public long RowsCount { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
