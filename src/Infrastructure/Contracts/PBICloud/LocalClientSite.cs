namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class LocalClientSite
    {
        public string? Url { get; init; }

        public string? Version { get; init; }

        public string? UserPrincipalName { get; init; }

        public string? DisplayName { get; init; }

        public string? Avatar { get; init; }
    }

    public class LocalClientSites : List<LocalClientSite>
    {
        public LocalClientSites(IEnumerable<LocalClientSite> sites)
            : base(sites)
        {
        }

        public LocalClientSite? Find(Uri url, string? upn)
        {
            if (upn is not null)
            {
                return this.FirstOrDefault((site) => url.AbsoluteUri.Equals(site.Url, StringComparison.OrdinalIgnoreCase) && upn.Equals(site.UserPrincipalName, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }
    }
}
