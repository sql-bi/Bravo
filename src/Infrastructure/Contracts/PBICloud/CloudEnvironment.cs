#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System;
    using System.Diagnostics;

    [DebuggerDisplay("{Name}, {AuthorityUri}")]
    public class CloudEnvironment
    { 
        public CloudEnvironmentType Name { get; init; }

        public Uri Authority { get; init; }

        public string ClientId { get; init; }

        public string[] Scopes { get; init; }

        public Uri Endpoint { get; init; }

        //public string RedirectUri { get; init; }

        //public string ResourceUri { get; init; }
    }
}
