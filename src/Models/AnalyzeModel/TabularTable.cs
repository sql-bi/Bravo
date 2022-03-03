namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using Dax.ViewModel;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json.Serialization;

    [DebuggerDisplay("{Name}")]
    public class TabularTable
    {
        [JsonPropertyName("features")]
        public TabularTableFeature Features { get; set; } = TabularTableFeature.All;

        [JsonPropertyName("featureUnsupportedReasons")]
        public TabularTableFeatureUnsupportedReason FeatureUnsupportedReasons { get; set; } = TabularTableFeatureUnsupportedReason.None;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("rowsCount")]
        public long RowsCount { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        internal static TabularTable CreateFrom(VpaTable vpaTable)
        {
            var table = new TabularTable
            {
                Name = vpaTable.TableName,
                RowsCount = vpaTable.RowsCount,
                Size = vpaTable.TableSize,
            };

            if (vpaTable.ColumnsNumber == 0L || (vpaTable.ColumnsNumber == 1L && vpaTable.Columns.Single().IsRowNumber))
            {
                table.Features &= ~TabularTableFeature.ExportData;
                table.FeatureUnsupportedReasons |= TabularTableFeatureUnsupportedReason.ExportDataNoColumns;
            }

            return table;
        }
    }

    [Flags]
    public enum TabularTableFeature
    {
        None = 0,

        // AnalyzeModel range << 100,
        // FormatDax range << 200,
        // ManageDates range << 300,

        ExportData = 1 << 400,

        All = ExportData,
    }

    [Flags]
    public enum TabularTableFeatureUnsupportedReason
    {
        None = 0,

        // AnalyzeModel range << 100,
        // FormatDax range << 200,
        // ManageDates range << 300,

        /// <summary>
        /// The table has no columns so it cannot be used as an export data source
        /// </summary>
        /// <example>
        /// https://github.com/sql-bi/Bravo/issues/128 "Query (%, %) Table '%' cannot be used in computations because it does not have any columns."
        /// </example> 
        ExportDataNoColumns = 1 << 400,
    }
}
