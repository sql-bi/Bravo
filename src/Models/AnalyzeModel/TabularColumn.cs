namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using System.Text.Json.Serialization;

    public class TabularColumn
    {
        [JsonPropertyName("columnName")]
        public string? Name { get; set; }

        [JsonPropertyName("tableName")]
        public string? TableName { get; set; }

        [JsonPropertyName("columnCardinality")]
        public long Cardinality { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("weight")]
        public double Weight { get; set; }

        [JsonPropertyName("isReferenced")]
        public bool IsReferenced { get; set; }
    }
}
