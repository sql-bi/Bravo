namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using System.Diagnostics;
    using System.Text.Json.Serialization;

    [DebuggerDisplay("'{TableName}'[{Name}]")]
    public class TabularColumn
    {

        [JsonPropertyName("name")]
        public string? TableColumnName { get; set; }

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

        [JsonPropertyName("dataType")]
        public string? DataType { get; set; }

        [JsonPropertyName("isHidden")]
        public bool IsHidden { get; set; }
    }
}
