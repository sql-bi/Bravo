namespace Sqlbi.Bravo.Infrastructure.Contracts.PBIDesktop
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Xml.Linq;

    internal class LocalClientSite
    {
        public string? Url { get; init; }

        public string? Version { get; init; }

        public string? UserPrincipalName { get; init; }

        public string? DisplayName { get; init; }

        public string? Avatar { get; init; }

        internal static LocalClientSite CreateFrom(XElement element)
        {
            var site = new LocalClientSite
            {
                Url = element.Attribute("Url")?.Value,
                Version = element.Attribute("Version")?.Value,
                UserPrincipalName = element.Element("User")?.Value.NullIfWhiteSpace(),
                DisplayName = element.Element("DisplayName")?.Value.NullIfWhiteSpace(),
                Avatar = element.Element("Avatar")?.Value.NullIfWhiteSpace(),
            };

            return site;
        }
    }

    internal class LocalClientSites
    {
        private static readonly string LocalDataClassicAppCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify), "Microsoft\\Power BI Desktop");
        private static readonly string LocalDataStoreAppCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), "Microsoft\\Power BI Desktop Store App");
        private const string UserCacheFile = "User.zip";

        private readonly List<LocalClientSite> _sites;

        private LocalClientSites(IEnumerable<LocalClientSite> sites)
        {
            _sites = sites.ToList();
        }

        public LocalClientSite? Find(Uri url, string? upn)
        {
            if (upn is not null)
            {
                return _sites.FirstOrDefault((site) => url.AbsoluteUri.Equals(site.Url, StringComparison.OrdinalIgnoreCase) && upn.Equals(site.UserPrincipalName, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        public static LocalClientSites? Create()
        {
            var classicAppCacheFile = new FileInfo(fileName: Path.Combine(LocalDataClassicAppCachePath, UserCacheFile));
            var storeAppCacheFile = new FileInfo(fileName: Path.Combine(LocalDataStoreAppCachePath, UserCacheFile));

            if (classicAppCacheFile.Exists && storeAppCacheFile.Exists)
            {
                var lastWritedCacheFile = classicAppCacheFile.LastWriteTime >= storeAppCacheFile.LastWriteTime ? classicAppCacheFile : storeAppCacheFile;

                if (TryGetFrom(lastWritedCacheFile.FullName, out var sites))
                    return sites;
            }

            if (classicAppCacheFile.Exists)
            {
                if (TryGetFrom(classicAppCacheFile.FullName, out var sites))
                    return sites;
            }

            if (storeAppCacheFile.Exists)
            {
                if (TryGetFrom(storeAppCacheFile.FullName, out var sites))
                    return sites;
            }

            return new LocalClientSites(Array.Empty<LocalClientSite>());

            static bool TryGetFrom(string file, [NotNullWhen(true)] out LocalClientSites? sites)
            {
                using var archive = ZipFile.OpenRead(file);
                var entry = archive.GetEntry("ClientAccess/ClientAccess.xml");

                if (entry is not null)
                {
                    using var reader = new StreamReader(entry.Open());
                    var document = XDocument.Load(reader);

                    var elements = document.Root?.Descendants("Sites").Descendants("Site");
                    if (elements is not null)
                    {
                        var latestSupportedVersion = new Version("2.9.0.0");
                        var items = elements.Select(LocalClientSite.CreateFrom).Where((s) => Version.TryParse(s.Version, out var version) && version >= latestSupportedVersion);

                        sites = new LocalClientSites(items);
                        return true;
                    }
                }

                sites = null;
                return false;
            }
        }
    }
}
