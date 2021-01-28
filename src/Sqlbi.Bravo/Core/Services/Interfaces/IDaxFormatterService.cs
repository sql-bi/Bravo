using Microsoft.AnalysisServices.Tabular;
using Sqlbi.Bravo.Core.Client.Http;
using Sqlbi.Bravo.Core.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IDaxFormatterService
    {
        Task InitilizeOrRefreshAsync(RuntimeSummary runtimeSummary);

        Task FormatAsync(DaxFormatterTabularObjectType objectType, RuntimeSummary runtimeSummary);

        void SaveFormattedMeasures(List<(string id, string expression)> measuresToUpdate, RuntimeSummary runtimeSummary);

        Task<Dictionary<string, (string, string)>> GetFormattedItems(DaxFormatterTabularObjectType objectType, RuntimeSummary runtimeSummary);

        int Count(DaxFormatterTabularObjectType objectType);

        List<Measure> GetMeasures();
    }
}
