using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Bravo.Models
{
    public class DatabaseModel
    {
        [JsonPropertyName("model")]
        public DatabaseModelInfo? Info { get; set; }

        [JsonPropertyName("measures")]
        public IEnumerable<DatabaseModelMeasure>? Measures { get; set; }
    }

    public class DatabaseModelInfo
    {
        [JsonPropertyName("tablesCount")]
        public int TablesCount { get; set; }

        [JsonPropertyName("columnsCount")]
        public int ColumnsCount { get; set; }

        [JsonPropertyName("maxRows")]
        public long TablesMaxRowsCount { get; set; }

        [JsonPropertyName("size")]
        public long DatabaseSize { get; set; }

        [JsonPropertyName("unreferencedCount")]
        public int ColumnsUnreferencedCount { get; set; }

        [JsonPropertyName("columns")]
        public IEnumerable<DatabaseModelColumn>? Columns { get; set; }
    }

    public class DatabaseModelColumn
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

    public class DatabaseModelMeasure
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("tableName")]
        public string? TableName { get; set; }

        [JsonPropertyName("measure")]
        public string? Expression { get; set; }
    }
}
