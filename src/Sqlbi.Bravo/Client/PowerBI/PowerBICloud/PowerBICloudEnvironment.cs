using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Client.PowerBI
{
    internal class PowerBICloudEnvironment
    { 
        public PowerBICloudEnvironmentType Name { get; init; }

        public string AuthorityUri { get; init; }

        public string ClientId { get; init; }

        public string RedirectUri { get; init; }

        public string[] Scopes { get; init; }

        public string ResourceUri { get; init; }

        public string BackendEndpointUri { get; init; }
    }
}
