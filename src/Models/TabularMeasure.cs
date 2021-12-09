using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class TabularMeasure
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("tableName")]
        public string? TableName { get; set; }

        [JsonPropertyName("measure")]
        public string? Expression { get; set; }
    }
}
