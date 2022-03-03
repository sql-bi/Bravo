namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using Sqlbi.Bravo.Models.FormatDax;
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using TOM = Microsoft.AnalysisServices;

    public class TabularDatabase
    {
        [JsonPropertyName("features")]
        public AppFeature Features { get; set; } = AppFeature.All;

        [JsonPropertyName("featureUnsupportedReasons")]
        public TabularDatabaseFeatureUnsupportedReason FeatureUnsupportedReasons { get; set; } = TabularDatabaseFeatureUnsupportedReason.None;

        [JsonPropertyName("model")]
        public TabularDatabaseInfo? Info { get; set; }

        [JsonPropertyName("measures")]
        public IEnumerable<TabularMeasure>? Measures { get; set; }
    }

    [Flags]
    public enum AppFeature // TODO: @daniele - breaking change, rename 'AppFeature' to 'TabularDatabaseFeature' see https://docs.microsoft.com/en-us/visualstudio/ide/reference/rename?view=vs-2022#how-to
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
        ManageDatesUpdateModel = 1 << 301,
        ManageDatesAll = ManageDatesPage | ManageDatesUpdateModel,

        ExportDataPage = 1 << 400,
        ExportDataSynchronize = 1 << 401,
        ExportDataAll = ExportDataPage | ExportDataSynchronize,

        AllUpdateModel = FormatDaxUpdateModel | ManageDatesUpdateModel,
        All = AnalyzeModelAll | FormatDaxAll | ManageDatesAll | ExportDataAll,
    }

    [Flags]
    public enum TabularDatabaseFeatureUnsupportedReason
    {
        None = 0,

        // AnalyzeModel range << 100,
        // FormatDax range << 200,

        /// <summary>
        /// Models with auto date/time option enabled are not supported, user must disable this option on the model before using ManageDate templates
        /// </summary>
        ManageDatesAutoDateTimeEnabled = 1 << 300,

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
