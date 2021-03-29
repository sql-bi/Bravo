using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models
{
    internal class GlobalServiceEnvironmentService
    {
        public string Name { get; set; }

        public string Endpoint { get; set; }

        public string ResourceId { get; set; }

        public IEnumerable<string> AllowedDomains { get; set; }

        public string AppId { get; set; }

        public string RedirectUri { get; set; }
    }
}
