namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using Dax.ViewModel;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Models.FormatDax;

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

        internal static TabularDatabase CreateFrom(Dax.Metadata.Model daxModel, TOM.Model? tomModel = default)
        {
            var vpaModel = new VpaModel(daxModel);

            var includedDaxTables = daxModel.Tables.Where((t) => !IsInternal(t)).ToArray();
            var includedDaxTableNames = includedDaxTables.Select((t) => t.TableName.Name).ToHashSet();

            var includedTables = vpaModel.Tables.Where((t) => includedDaxTableNames.Contains(t.TableName)).ToArray();
            var includedColumns = includedTables.SelectMany((t) => t.Columns.Where((c) => !c.IsRowNumber)).ToArray();
            var includedMeasures = daxModel.Tables.SelectMany((t) => t.Measures).ToArray(); // Measures here are not filtered as they are always considered 'included'

            var databaseETag = TabularModelHelper.GetDatabaseETag(vpaModel.Model.ModelName.Name, vpaModel.Model.Version, vpaModel.Model.LastUpdate);
            var databaseSize = includedColumns.Sum((c) => c.TotalSize);
            var tables = includedTables.Select((t) => TabularTable.CreateFrom(t, tomModel)).ToArray();
            var columns = includedColumns.Select((c) => TabularColumn.CreateFrom(c, databaseSize)).ToArray();
            var measures = includedMeasures.Select((m) => TabularMeasure.CreateFrom(m, databaseETag, tomModel)).ToArray();
            var autoLineBreakStyle = measures.GetAutoLineBreakStyle();

            var database = new TabularDatabase
            {
                Info = new TabularDatabaseInfo
                {
                    IsObfuscated = daxModel.ObfuscatorDictionaryId is not null,
                    ETag = databaseETag,
                    Name = daxModel.ModelName.Name,
                    Culture = tomModel?.Culture,
                    CompatibilityMode = daxModel.CompatibilityMode.TryParseTo<SSAS.CompatibilityMode>(),
                    CompatibilityLevel = daxModel.CompatibilityLevel,
                    DatabaseSize = databaseSize,
                    AutoLineBreakStyle = autoLineBreakStyle,
                    ServerName = daxModel.ServerName?.Name ?? tomModel?.Server.Name,
                    ServerVersion = tomModel?.Server.Version,
                    ServerEdition = tomModel?.Server.Edition,
                    ServerMode = tomModel?.Server.ServerMode,
                    ServerLocation = tomModel?.Server.ServerLocation,
                    TablesMaxRowsCount = includedTables.Length == 0 ? 0L : includedTables.Max((t) => t.RowsCount),
                    TablesCount = includedTables.Length,
                    Tables = tables,
                    ColumnsUnreferencedCount = includedColumns.Count((c) => !c.IsReferenced),
                    ColumnsCount = includedColumns.Length,
                    Columns = columns,
                },
                Measures = measures
            };

            if (daxModel.Tables.Any(IsAutoDateTime))
            {
                database.Features &= ~TabularDatabaseFeature.ManageDatesAll;
                database.FeatureUnsupportedReasons |= TabularDatabaseFeatureUnsupportedReason.ManageDatesAutoDateTimeEnabled;
            }

            if (daxModel.Tables.Count == 0)
            {
                database.Features &= ~TabularDatabaseFeature.ManageDatesAll;
                database.FeatureUnsupportedReasons |= TabularDatabaseFeatureUnsupportedReason.ManageDatesEmptyTableCollection;
            }

            if (tomModel is null || tomModel.Database.ReadWriteMode == SSAS.ReadWriteMode.ReadOnly)
            {
                database.Features &= ~TabularDatabaseFeature.AllUpdateModel;
                database.FeatureUnsupportedReasons |= TabularDatabaseFeatureUnsupportedReason.ReadOnly;
            }

            return database;

            static bool IsInternal(Dax.Metadata.Table daxTable) => daxTable.IsPrivate || IsAutoDateTime(daxTable);

            static bool IsAutoDateTime(Dax.Metadata.Table daxTable) => daxTable.IsLocalDateTable || daxTable.IsTemplateDateTable;
        }

        internal static TabularDatabase CreateFrom(AdomdConnectionWrapper connection)
        {
            using var tablesWithColumnsCommand = connection.CreateDmvTablesWithColumnsCommand();
            using var tablesWithColumnsReader = tablesWithColumnsCommand.ExecuteReader(CommandBehavior.SingleResult);
            var tablesWithColumns = tablesWithColumnsReader.Select((reader) => ((string)reader["DIMENSION_UNIQUE_NAME"]).GetDaxName()!).ToArray();

            using var tablesCommand = connection.CreateDmvTablesCommand();
            using var tablesReader = tablesCommand.ExecuteReader(CommandBehavior.SingleResult);
            var tables = tablesReader.Select((reader) => TabularTable.CreateFromDmvTables(reader, tablesWithColumns)).Where((t) => !IsInternal(t)).ToArray();

            var database = new TabularDatabase
            {
                Info = new TabularDatabaseInfo
                {
                    IsObfuscated = false,
                    ETag = null,
                    Name = connection.Connection.Database,
                    CompatibilityMode = null,
                    CompatibilityLevel = null,
                    DatabaseSize = null,
                    AutoLineBreakStyle = null,
                    ServerName = null,
                    ServerVersion = null,
                    ServerEdition = null,
                    ServerMode = null,
                    ServerLocation = null,
                    TablesMaxRowsCount = tables.Length == 0L ? 0L : tables.Max((t) => t.RowsCount),
                    TablesCount = tables.Length,
                    Tables = tables,
                    ColumnsUnreferencedCount = 0,
                    ColumnsCount = 0,
                    Columns = Array.Empty<TabularColumn>(),
                },
                Measures = Array.Empty<TabularMeasure>(),
            };

            database.Features &= ~TabularDatabaseFeature.AnalyzeModelAll;
            database.Features &= ~TabularDatabaseFeature.FormatDaxAll;
            database.FeatureUnsupportedReasons |= TabularDatabaseFeatureUnsupportedReason.XmlaEndpointNotSupported;

            return database;

            static bool IsInternal(TabularTable table) => table.Name.IsAutoDateTimeTableName();
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

        /// <summary>
        /// The XMLA endpoint is not supported for the <see cref="PBICloudDataset"/> workspace capacity SKU
        /// </summary>
        /// <remarks>
        /// The XMLA endpoint is available for Power BI Premium Capacity workspaces (i.e. workspaces assigned to a Px, Ax or EMx SKU), Power BI Embedded workspaces, or Power BI Premium-Per-User (PPU) workspaces
        /// </remarks>
        XmlaEndpointNotSupported = 1 << 3,

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

         /// <summary>
        /// Feature supported only by databases that have at least one table
        /// </summary>
        ManageDatesEmptyTableCollection = 1 << 302,

        // ExportData range << 400,
    }

    public class TabularDatabaseInfo
    {
        [JsonPropertyName("isObfuscated")]
        public bool IsObfuscated { get; set; }

        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("culture")]
        public string? Culture { get; set; }

        [JsonPropertyName("compatibilityMode")]
        public SSAS.CompatibilityMode? CompatibilityMode { get; set; }

        [JsonPropertyName("compatibilityLevel")]
        public int? CompatibilityLevel { get; set; }

        [JsonPropertyName("serverName")]
        public string? ServerName { get; set; }

        [JsonPropertyName("serverVersion")]
        public string? ServerVersion { get; set; }

        [JsonPropertyName("serverEdition")]
        public SSAS.ServerEdition? ServerEdition { get; set; }

        [JsonPropertyName("serverMode")]
        public SSAS.ServerMode? ServerMode { get; set; }

        [JsonPropertyName("serverLocation")]
        public SSAS.ServerLocation? ServerLocation { get; set; }

        [JsonPropertyName("tablesCount")]
        public int? TablesCount { get; set; }

        [JsonPropertyName("columnsCount")]
        public int? ColumnsCount { get; set; }

        [JsonPropertyName("maxRows")]
        public long? TablesMaxRowsCount { get; set; }

        [JsonPropertyName("size")]
        public long? DatabaseSize { get; set; }

        [JsonPropertyName("unreferencedCount")]
        public int? ColumnsUnreferencedCount { get; set; }

        [JsonPropertyName("autoLineBreakStyle")]
        public DaxLineBreakStyle? AutoLineBreakStyle { get; set; }

        [JsonPropertyName("columns")]
        public IEnumerable<TabularColumn>? Columns { get; set; }

        [JsonPropertyName("tables")]
        public IEnumerable<TabularTable>? Tables { get; set; }
    }
}
