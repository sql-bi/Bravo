using Sqlbi.Bravo.Core.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IDaxFormatterService
    {
        Task InitilizeOrRefreshAsync(RuntimeSummary runtimeSummary);

        IEnumerable<DaxFormatterServiceTabularMeasure> Measures { get; }

        Task<IEnumerable<IDaxFormatterServiceTabularObject>> FormatAsync(IList<IDaxFormatterServiceTabularObject> tabularObjects);

        Task ApplyFormatAsync(IList<IDaxFormatterServiceTabularObject> tabularObjects);
    }
}