namespace Sqlbi.Bravo.Services
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Contracts.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Mime;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IPBICloudSettingsService
    {
        CloudEnvironment CloudEnvironment { get; }

        TenantCluster TenantCluster { get; }

        Task InitializeAsync();

        Task RefreshAsync(string accessToken);
    }

    internal class PBICloudSettingsService : IPBICloudSettingsService
    {
        private const string GlobalServiceEnvironmentsDiscoverUrl = "powerbi/globalservice/v201606/environments/discover?client=powerbi-msolap";
        private const string GlobalServiceGetOrInsertClusterUrisByTenantlocationUrl = "spglobalservice/GetOrInsertClusterUrisByTenantlocation";
        private const string CloudEnvironmentGlobalCloudName = "GlobalCloud";

        private readonly static SemaphoreSlim _initializeSemaphore = new(1, 1);
        private readonly HttpClient _httpClient;

        private GlobalService? _globalService;
        private CloudEnvironment? _cloudEnvironment;
        private TenantCluster? _tenantCluster;

        public PBICloudSettingsService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.BaseAddress = PBICloudService.PBIApiUri;
        }

        public CloudEnvironment CloudEnvironment
        {
            get
            {
                BravoUnexpectedException.ThrowIfNull(_cloudEnvironment);
                return _cloudEnvironment;
            }
        }

        public TenantCluster TenantCluster
        {
            get
            {
                BravoUnexpectedException.ThrowIfNull(_tenantCluster);
                return _tenantCluster;
            }
        }

        /// <remarks>Refresh is required only if the login account has changed</remarks>
        public async Task RefreshAsync(string accessToken)
        {
            _tenantCluster = await GetTenantClusterAsync(accessToken).ConfigureAwait(false);
            //_localClientSites = await RefreshLocalClientSitesAsync().ConfigureAwait(false);
        }

        public async Task InitializeAsync()
        {
            if (_globalService is null || _cloudEnvironment is null)
            {
                await _initializeSemaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_globalService is null)
                        _globalService = await InitializeGlobalServiceAsync().ConfigureAwait(false);

                    if (_cloudEnvironment is null)
                        _cloudEnvironment = InitializeGlobalCloudEnvironment();
                }
                finally
                {
                    _initializeSemaphore.Release();
                }
            }
        }

        private async Task<GlobalService> InitializeGlobalServiceAsync()
        {
            var securityProtocol = ServicePointManager.SecurityProtocol;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                using var request = new HttpRequestMessage(HttpMethod.Post, GlobalServiceEnvironmentsDiscoverUrl);
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var globalService = JsonSerializer.Deserialize<GlobalService>(json, AppEnvironment.DefaultJsonOptions); BravoUnexpectedException.ThrowIfNull(globalService);

                return globalService;
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }
        }

        private CloudEnvironment InitializeGlobalCloudEnvironment()
        {
            var environmentName = CloudEnvironmentGlobalCloudName;
            var globalEnvironment = _globalService?.Environments.SingleOrDefault((c) => environmentName.EqualsI(c.CloudName)); BravoUnexpectedException.ThrowIfNull(globalEnvironment);

            var authority = globalEnvironment.Services.Single((s) => "aad".EqualsI(s.Name));
            var frontend = globalEnvironment.Services.Single((s) => "powerbi-frontend".EqualsI(s.Name));
            var backend = globalEnvironment.Services.Single((s) => "powerbi-backend".EqualsI(s.Name));
            var gateway = globalEnvironment.Clients.Single((c) => "powerbi-gateway".EqualsI(c.Name));

            var cloudEnvironment = new CloudEnvironment
            {
                Name = CloudEnvironmentType.Public,
                Authority = new Uri(authority.Endpoint, UriKind.Absolute),
                ClientId = gateway.AppId,
                Scopes = new string[] { $"{ backend.ResourceId }/.default" },
                Endpoint = new Uri(frontend.Endpoint, UriKind.Absolute),
                //RedirectUri = gateway.RedirectUri,
                //ResourceUri = backend.ResourceId,
            };

            return cloudEnvironment;
        }

        private async Task<TenantCluster> GetTenantClusterAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var request = new HttpRequestMessage(HttpMethod.Put, GlobalServiceGetOrInsertClusterUrisByTenantlocationUrl)
            {
                Content = new StringContent(string.Empty, Encoding.UTF8, MediaTypeNames.Application.Json)
            };

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var tenantCluster = JsonSerializer.Deserialize<TenantCluster>(json, AppEnvironment.DefaultJsonOptions); BravoUnexpectedException.ThrowIfNull(tenantCluster);

            return tenantCluster;
        }
        /*
                private async Task RefreshLocalClientSitesAsync()
                {
                    static readonly string LocalDataClassicAppCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify), "Microsoft\\Power BI Desktop");
                    static readonly string LocalDataStoreAppCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), "Microsoft\\Power BI Desktop Store App");

                    const string UserCacheFile = "User.zip";

                    var classicAppCacheFile = new FileInfo(fileName: Path.Combine(LocalDataClassicAppCachePath, UserCacheFile));
                    var storeAppCacheFile = new FileInfo(fileName: Path.Combine(LocalDataStoreAppCachePath, UserCacheFile));

                    if (classicAppCacheFile.Exists && storeAppCacheFile.Exists)
                    {
                        var lastWritedCacheFile = classicAppCacheFile.LastWriteTime >= storeAppCacheFile.LastWriteTime ? classicAppCacheFile : storeAppCacheFile;
                        if ((_localClientSites = GetLocalClientSites(lastWritedCacheFile.FullName)) is not null)
                            return;
                    }

                    if (classicAppCacheFile.Exists)
                    {
                        if ((_localClientSites = GetLocalClientSites(classicAppCacheFile.FullName)) is not null)
                            return;
                    }

                    if (storeAppCacheFile.Exists)
                    {
                        if ((_localClientSites = GetLocalClientSites(storeAppCacheFile.FullName)) is not null)
                            return;
                    }

                    _localClientSites = null;
                    await Task.CompletedTask;

                    static LocalClientSites? GetLocalClientSites(string archiveFile)
                    {
                        Version LatestSupportedVersion = new("2.9.0.0");

                        using var archive = ZipFile.OpenRead(archiveFile);
                        var entry = archive.GetEntry("ClientAccess/ClientAccess.xml");
                        if (entry is not null)
                        {
                            using var reader = new StreamReader(entry.Open());
                            var document = XDocument.Load(reader);

                            var elements = document.Root?.Descendants("Sites").Descendants("Site");
                            if (elements is not null)
                            {
                                var sites = elements.Select((e) => new LocalClientSite
                                {
                                    Url = e.Attribute("Url")?.Value,
                                    Version = e.Attribute("Version")?.Value,
                                    UserPrincipalName = e.Element("User")?.Value.NullIfWhiteSpace(),
                                    DisplayName = e.Element("DisplayName")?.Value.NullIfWhiteSpace(),
                                    Avatar = e.Element("Avatar")?.Value.NullIfWhiteSpace(),
                                })
                                .Where((s) => Version.TryParse(s.Version, out var version) && version >= LatestSupportedVersion);

                                var clientSites = new LocalClientSites(sites);
                                if (clientSites.Count > 0)
                                    return clientSites;
                            }
                        }

                        return null;
                    }
                }
        */
    }
}
