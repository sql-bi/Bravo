#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System.Collections.Generic;

    public class GlobalService
    {
        public IEnumerable<GlobalServiceEnvironment> Environments { get; set; }
    }
}
