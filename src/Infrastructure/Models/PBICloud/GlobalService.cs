using System.Collections.Generic;

#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    public class GlobalService
    {
        public IEnumerable<GlobalServiceEnvironment> Environments { get; set; }
    }
}
