using Sqlbi.Bravo.Core.Settings;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IDaxFormatterService
    {
        Task InitilizeOrRefreshAsync(RuntimeSummary runtimeSummary);

        IEnumerable<DaxFormatterServiceTabularMeasure> Measures { get; }

        Task<IEnumerable<IDaxFormatterServiceTabularObject>> FormatAsync(IList<IDaxFormatterServiceTabularObject> tabularObjects, IDaxFormatterSettings settings);

        Task ApplyFormatAsync(IList<IDaxFormatterServiceTabularObject> tabularObjects);
    }
}