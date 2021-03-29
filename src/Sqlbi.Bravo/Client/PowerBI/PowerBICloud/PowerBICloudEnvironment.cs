using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Client.PowerBI
{
    internal class PowerBICloudEnvironment
    { 
        public PowerBICloudEnvironmentType Name { get; set; }

        public string AuthorityUri { get; set; }

        public string ClientId { get; set; }

        public string RedirectUri { get; set; }

        public string[] Scopes { get; set; }
    }
}
