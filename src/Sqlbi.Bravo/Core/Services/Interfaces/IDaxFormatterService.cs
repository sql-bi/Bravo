using Sqlbi.Bravo.Client.DaxFormatter;
using Sqlbi.Bravo.Client.DaxFormatter.Interfaces;
using Sqlbi.Bravo.Core.Settings;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IDaxFormatterService
    {
        Task InitilizeOrRefreshAsync(RuntimeSummary runtimeSummary);

        IEnumerable<TabularMeasure> Measures { get; }

        Task<IEnumerable<ITabularObject>> FormatAsync(IList<ITabularObject> tabularObjects, IDaxFormatterSettings settings);

        Task ApplyFormatAsync(IList<ITabularObject> tabularObjects);
    }
}