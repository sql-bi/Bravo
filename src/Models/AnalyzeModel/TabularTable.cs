namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using Dax.ViewModel;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Services.DaxTemplate;
    using System;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json.Serialization;
    using TOM = Microsoft.AnalysisServices.Tabular;

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
        public long? RowsCount { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }

        [JsonPropertyName("isDateTable")]
        public bool? IsDateTable { get; set; }

        [JsonPropertyName("isHidden")]
        public bool? IsHidden { get; set; }

        [JsonPropertyName("isQueryable")]
        public bool? IsQueryable { get; set; }

        [JsonPropertyName("isManageDates")]
        public bool? IsManageDates { get; set; }

        internal static TabularTable CreateFromDmvTables(IDataReader reader, string[] tablesWithColumns)
        {
            var table = new TabularTable
            {
                Name = (string)reader["DIMENSION_UNIQUE_NAME"],
                RowsCount = (uint)reader["DIMENSION_CARDINALITY"],
                // Just using the 'DIMENSION_TYPE' property returns an incomplete result compared to the one obtained via VertipaqAnalyzer, so the result may be different in some cases
                IsDateTable = ((short)reader["DIMENSION_TYPE"]) == 1, /* MD_DIMTYPE_TIME */ 
            };

            table.Name = table.Name.GetDaxName();

            if (!tablesWithColumns.Contains(table.Name))
            {
                table.Features &= ~TabularTableFeature.ExportData;
                table.FeatureUnsupportedReasons |= TabularTableFeatureUnsupportedReason.ExportDataNoColumns;
            }

            return table;
        }

        internal static TabularTable CreateFrom(VpaTable vpaTable, TOM.Model? tomModel = default)
        {
            var table = new TabularTable
            {
                Name = vpaTable.TableName,
                RowsCount = vpaTable.RowsCount,
                Size = vpaTable.TableSize,
                IsDateTable = vpaTable.IsDateTable,
                IsHidden = null,
                IsQueryable = null,
                IsManageDates = null
            };

            var tomTable = tomModel?.Tables.Find(table.Name);
            if (tomTable is not null)
            {
                table.IsHidden = tomTable.IsHidden;
                table.IsQueryable = tomTable.IsQueryable();
                table.IsManageDates = tomTable.Annotations.Contains(DaxTemplateManager.SqlbiTemplateAnnotation);
            }

            if (table.IsQueryable == false)
            {
                table.Features &= ~TabularTableFeature.ExportData;
                table.FeatureUnsupportedReasons |= TabularTableFeatureUnsupportedReason.ExportDataNotQueryable;
            }

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

        /// <summary>
        /// The table has columns that are not queryable (i.e. TOM.ObjectState IN CalculationNeeded, SemanticError, EvaluationError, SyntaxError, ...)
        /// </summary> 
        ExportDataNotQueryable = 1 << 401,
    }
}
