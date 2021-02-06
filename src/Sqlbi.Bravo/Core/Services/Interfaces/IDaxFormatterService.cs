using Microsoft.AnalysisServices.Tabular;
using Sqlbi.Bravo.Core.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IDaxFormatterService
    {
        Task InitilizeOrRefreshAsync(RuntimeSummary runtimeSummary);

        Task FormatAsync(DaxFormatterServiceTabularObjectType objectType, RuntimeSummary runtimeSummary);

        void SaveFormattedMeasures(List<(string id, string expression)> measuresToUpdate, RuntimeSummary runtimeSummary);

        Task<Dictionary<string, (string, string)>> GetFormattedItems(DaxFormatterServiceTabularObjectType objectType, RuntimeSummary runtimeSummary);

        int Count(DaxFormatterServiceTabularObjectType objectType);

        List<Measure> GetMeasures();
    }
}
