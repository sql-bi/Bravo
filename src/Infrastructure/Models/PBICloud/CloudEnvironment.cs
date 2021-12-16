#nullable disable

using System.Diagnostics;

namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{

    [DebuggerDisplay("Name = {Name}, AuthorityUri = {AuthorityUri}")]
    public class CloudEnvironment
    { 
        public CloudEnvironmentType Name { get; init; }

        public string AuthorityUri { get; init; }

        public string ClientId { get; init; }

        public string RedirectUri { get; init; }

        public string[] Scopes { get; init; }

        public string ResourceUri { get; init; }

        public string EndpointUri { get; init; }
    }
}
