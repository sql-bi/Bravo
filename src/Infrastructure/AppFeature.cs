namespace Sqlbi.Bravo.Infrastructure
{
    using System;

    [Flags]
    public enum AppFeature
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
}
