using Dax.Metadata;
using Dax.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IAnalyzeModelService
    {
        Task InitilizeOrRefreshAsync();

        (long DatasetSize, int ColumnCount) GetDatasetSummary();

        List<VpaColumn> GetUnusedColumns();

        IEnumerable<VpaColumn> GetAllColumns();

        IEnumerable<VpaTable> GetAllTables();

        DateTime GetLastSyncTime();

        Model GetModelForExport();
    }
}
