namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class TabularDatabase
    {
        [JsonPropertyName("model")]
        public TabularDatabaseInfo? Info { get; set; }

        [JsonPropertyName("measures")]
        public IEnumerable<TabularMeasure>? Measures { get; set; }
    }

    public class TabularDatabaseInfo
    {
        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

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
        public IEnumerable<TabularColumn>? Columns { get; set; }

        [JsonPropertyName("tables")]
        public IEnumerable<TabularTable>? Tables { get; set; }
    }
}
