namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Models.FormatDax;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using TOM = Microsoft.AnalysisServices;

    public class TabularDatabase
    {
        [JsonPropertyName("features")]
        public AppFeature Features { get; set; } = AppFeature.All;

        [JsonPropertyName("model")]
        public TabularDatabaseInfo? Info { get; set; }

        [JsonPropertyName("measures")]
        public IEnumerable<TabularMeasure>? Measures { get; set; }
    }

    public class TabularDatabaseInfo
    {
        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("compatibilityMode")]
        public TOM.CompatibilityMode? CompatibilityMode { get; set; }

        [JsonPropertyName("compatibilityLevel")]
        public int? CompatibilityLevel { get; set; }

        [JsonPropertyName("serverName")]
        public string? ServerName { get; set; }

        [JsonPropertyName("serverVersion")]
        public string? ServerVersion { get; set; }

        [JsonPropertyName("serverEdition")]
        public TOM.ServerEdition? ServerEdition { get; set; }

        [JsonPropertyName("serverMode")]
        public TOM.ServerMode? ServerMode { get; set; }

        [JsonPropertyName("serverLocation")]
        public TOM.ServerLocation? ServerLocation { get; set; }

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

        [JsonPropertyName("autoLineBreakStyle")]
        public DaxLineBreakStyle? AutoLineBreakStyle { get; set; }

        [JsonPropertyName("columns")]
        public IEnumerable<TabularColumn>? Columns { get; set; }

        [JsonPropertyName("tables")]
        public IEnumerable<TabularTable>? Tables { get; set; }
    }
}
