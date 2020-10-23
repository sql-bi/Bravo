using System.Collections.Generic;

namespace Sqlbi.Bravo.Core.Client.Http
{
    internal class DaxFormatterResponse
    {
        public string Formatted { get; set; }

        public List<DaxFormatterError> Errors { get; set; }
    }
}
