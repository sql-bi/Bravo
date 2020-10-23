using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Client.Http.Interfaces
{
    internal interface IDaxFormatterHttpClient
    {
        Task<DaxFormatterResponse> FormatAsync(DaxFormatterRequest request);

        IAsyncEnumerable<DaxFormatterResponse> FormatAsync(IEnumerable<DaxFormatterRequest> requests);
    }
}
