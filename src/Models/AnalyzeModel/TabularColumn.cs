namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using Dax.ViewModel;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System.Diagnostics;
    using System.Text.Json.Serialization;
    using TOM = Microsoft.AnalysisServices.Tabular;

    [DebuggerDisplay("'{TableName}'[{Name}]")]
    public class TabularColumn
    {
        [JsonPropertyName("name")]
        public string? FullName => $"'{ TableName }'[{ Name }]";

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

        [JsonPropertyName("isQueryable")]
        public bool? IsQueryable { get; set; }

        internal static TabularColumn CreateFrom(VpaColumn vpaColumn, long databaseSize)
        {
            // Prevent division by zero or invalid floating-point results when the database size is zero. 
            // NaN or Infinity values are invalid in JSON and cause serialization errors during API responses. 
            // This may occur when VPA extracts zero values for column sizes, which has been observed 
            // in models with Direct Lake partitions where the DMVs DISCOVER_STORAGE_TABLES, 
            // DISCOVER_STORAGE_TABLE_COLUMNS, and DISCOVER_STORAGE_TABLE_COLUMN_SEGMENTS may return null/zero.
            // See https://github.com/sql-bi/VertiPaq-Analyzer/pull/196 
            // See https://github.com/sql-bi/VertiPaq-Analyzer/pull/209
            // This check is retained for backward compatibility and to handle possible regressions, 
            // as the issue was previously fixed in the PBI service but appears to have reoccurred, 
            // resulting in the error reported in https://github.com/sql-bi/Bravo/issues/903.
            double weight = 0;
            if (databaseSize > 0)
                weight = (double)vpaColumn.TotalSize / databaseSize;

            var column = new TabularColumn
            {
                Name = vpaColumn.ColumnName,
                TableName = vpaColumn.Table.TableName,
                Cardinality = vpaColumn.ColumnCardinality,
                Size = vpaColumn.TotalSize,
                Weight = weight,
                IsReferenced = vpaColumn.IsReferenced,
                DataType = vpaColumn.DataType,
                IsHidden = vpaColumn.IsHidden,
                IsQueryable = vpaColumn.State.TryParseTo<TOM.ObjectState>()?.IsQueryable(),
            };

            return column;
        }
    }
}
