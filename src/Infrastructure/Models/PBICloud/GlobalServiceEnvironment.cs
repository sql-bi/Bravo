using System.Collections.Generic;
using System.Diagnostics;

#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    [DebuggerDisplay("{CloudName}")]
    public class GlobalServiceEnvironment
    {
        public string CloudName { get; set; }

        public IEnumerable<GlobalServiceEnvironmentService> Services { get; set; }

        public IEnumerable<GlobalServiceEnvironmentClient> Clients { get; set; }
    }
}
