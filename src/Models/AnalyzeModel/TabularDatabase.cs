namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using Dax.ViewModel;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models.FormatDax;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;
    using TOM = Microsoft.AnalysisServices;

    public class TabularDatabase
    {
        [JsonPropertyName("features")]
        public TabularDatabaseFeature Features { get; set; } = TabularDatabaseFeature.All;

        [JsonPropertyName("featureUnsupportedReasons")]
        public TabularDatabaseFeatureUnsupportedReason FeatureUnsupportedReasons { get; set; } = TabularDatabaseFeatureUnsupportedReason.None;

        [JsonPropertyName("model")]
        public TabularDatabaseInfo? Info { get; set; }

        [JsonPropertyName("measures")]
        public IEnumerable<TabularMeasure>? Measures { get; set; }

        internal static TabularDatabase CreateFrom(Dax.Metadata.Model daxModel)
        {
            var vpaModel = new VpaModel(daxModel);

            var includedDaxTables = daxModel.Tables.Where(IsIncluded).ToArray();
            var includedDaxTableNames = includedDaxTables.Select((t) => t.TableName.Name).ToHashSet();

            var includedTables = vpaModel.Tables.Where((t) => includedDaxTableNames.Contains(t.TableName)).ToArray();
            var includedColumns = includedTables.SelectMany((t) => t.Columns.Where((c) => !c.IsRowNumber)).ToArray();
            var includedMeasures = daxModel.Tables.SelectMany((t) => t.Measures).ToArray(); // Measures here are not filtered as they are always considered 'included'

            var databaseETag = TabularModelHelper.GetDatabaseETag(vpaModel.Model.ModelName.Name, vpaModel.Model.Version, vpaModel.Model.LastUpdate);
            var databaseSize = includedColumns.Sum((c) => c.TotalSize);
            var tabularTables = includedTables.Select((t) => TabularTable.CreateFrom(t, daxModel)).ToArray();
            var tabularColumns = includedColumns.Select((c) => TabularColumn.CreateFrom(c, databaseSize)).ToArray();
            var tabularMeasures = includedMeasures.Select((m) => TabularMeasure.CreateFrom(m, databaseETag)).ToArray();
            var autoLineBreakStyle = tabularMeasures.GetAutoLineBreakStyle();

            var tabularDatabase = new TabularDatabase
            {
                Info = new TabularDatabaseInfo
                {
                    ETag = databaseETag,
                    Name = daxModel.ModelName.Name,
                    CompatibilityMode = daxModel.CompatibilityMode.TryParseTo<TOM.CompatibilityMode>(),
                    CompatibilityLevel = daxModel.CompatibilityLevel,
                    DatabaseSize = databaseSize,
                    AutoLineBreakStyle = autoLineBreakStyle,
                    ServerName = daxModel.ServerName.Name,
                    ServerVersion = null,
                    ServerEdition = null,
                    ServerMode = null,
                    ServerLocation = null,
                    TablesMaxRowsCount = includedTables.Length == 0 ? 0 : includedTables.Max((t) => t.RowsCount),
                    TablesCount = includedTables.Length,
                    Tables = tabularTables,
                    ColumnsUnreferencedCount = includedColumns.Count((c) => !c.IsReferenced),
                    ColumnsCount = includedColumns.Length,
                    Columns = tabularColumns,
                },
                Measures = tabularMeasures
            };

            if (daxModel.Tables.Any(IsAutoDateTimeTable))
            {
                tabularDatabase.Features &= ~TabularDatabaseFeature.ManageDatesAll;
                tabularDatabase.FeatureUnsupportedReasons |= TabularDatabaseFeatureUnsupportedReason.ManageDatesAutoDateTimeEnabled;
            }

            return tabularDatabase;

            static bool IsAutoDateTimeTable(Dax.Metadata.Table daxTable)
            {
                if (daxTable.IsLocalDateTable || daxTable.IsTemplateDateTable)
                    return true;

                return false;
            }

            static bool IsIncluded(Dax.Metadata.Table daxTable)
            {
                if (daxTable.IsPrivate)
                    return false;

                if (IsAutoDateTimeTable(daxTable))
                    return false;

                return true;
            }
        }
    }

    [Flags]
    public enum TabularDatabaseFeature
    {
        None = 0,

        AnalyzeModelPage = 1 << 100,
        AnalyzeModelSynchronize = 1 << 101,
        AnalyzeModelExportVpax = 1 << 102,
        AnalyzeModelAll = AnalyzeModelPage | AnalyzeModelSynchronize | AnalyzeModelExportVpax,

        FormatDaxPage = 1 << 200,
        FormatDaxSynchronize = 1 << 201,
        FormatDaxUpdateModel = 1 << 202,
        FormatDaxAll = FormatDaxPage | FormatDaxSynchronize | FormatDaxUpdateModel,

        ManageDatesPage = 1 << 300,
        ManageDatesSynchronize = 1 << 301,
        ManageDatesUpdateModel = 1 << 302,
        ManageDatesAll = ManageDatesPage | ManageDatesSynchronize | ManageDatesUpdateModel,

        ExportDataPage = 1 << 400,
        ExportDataSynchronize = 1 << 401,
        ExportDataAll = ExportDataPage | ExportDataSynchronize,

        AllSynchronize = AnalyzeModelSynchronize | FormatDaxSynchronize | ManageDatesSynchronize | ExportDataSynchronize,
        AllUpdateModel = FormatDaxUpdateModel | ManageDatesUpdateModel,
        All = AnalyzeModelAll | FormatDaxAll | ManageDatesAll | ExportDataAll,
    }

    [Flags]
    public enum TabularDatabaseFeatureUnsupportedReason
    {
        None = 0,

        /// <summary>
        /// The state of the connected database instance is read-only
        /// </summary>
        ReadOnly = 1 << 1,

        /// <summary>
        /// The <see cref="TabularDatabase"/> was generated from a VPAX file that is a representation of the Tabular model and includes only its metadata
        /// </summary>
        MetadataOnly = 1 << 2,

        // AnalyzeModel range << 100,
        // FormatDax range << 200,

        /// <summary>
        /// Models with auto date/time option enabled are not supported, user must disable this option on the model before using ManageDate templates
        /// </summary>
        ManageDatesAutoDateTimeEnabled = 1 << 300,

        /// <summary>
        /// Feature supported only for models in Power BI Desktop mode
        /// </summary>
        ManageDatesPBIDesktopModelOnly = 1 << 301,

        // ExportData range << 400,
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
