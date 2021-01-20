using Microsoft.AnalysisServices.Tabular;
using Sqlbi.Bravo.Core.Client.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IAnalyzeModelService
    {
        Task InitilizeOrRefreshAsync();
    }

    internal interface IDaxFormatterService
    {
        Task InitilizeOrRefreshAsync();

        Task FormatAsync(DaxFormatterTabularObjectType objectType);

        void SaveFormattedMeasures(List<(string id, string expression)> measuresToUpdate);

        Task<Dictionary<string, (string, string)>> GetFormattedItems(DaxFormatterTabularObjectType objectType);

        int Count(DaxFormatterTabularObjectType objectType);

        List<Measure> GetMeasures();
    }
}
