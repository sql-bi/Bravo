using Dax.ViewModel;
using Sqlbi.Bravo.Core.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IAnalyzeModelService
    {
        Task InitilizeOrRefreshAsync(ConnectionSettings connectionSettings);

        Task ExportVertiPaqAnalyzerModel(string path);

        (long DatasetSize, int ColumnCount) DatasetSummary { get; }

        IEnumerable<VpaColumn> UnusedColumns { get; }

        IEnumerable<VpaColumn> AllColumns { get; }

        IEnumerable<VpaTable> AllTables { get; }

        DateTime LastSyncTime { get; }
    }
}
