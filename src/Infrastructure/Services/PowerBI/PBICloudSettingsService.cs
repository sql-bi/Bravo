namespace Sqlbi.Bravo.Infrastructure.Services.PowerBI
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Contracts.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
    using Sqlbi.Bravo.Models;
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
        IPBICloudEnvironment CloudEnvironment { get; }

        TenantCluster TenantCluster { get; }

        Task InitializeAsync();

        Task RefreshAsync(string accessToken);
    }

    internal class PBICloudSettingsService : IPBICloudSettingsService
    {
        private const string GlobalServiceEnvironmentsDiscoverUrl = "powerbi/globalservice/v201606/environments/discover?client=powerbi-msolap";
        private const string GlobalServiceGetOrInsertClusterUrisByTenantlocationUrl = "spglobalservice/GetOrInsertClusterUrisByTenantlocation";

        private readonly static SemaphoreSlim _initializeSemaphore = new(1, 1);
        private readonly HttpClient _httpClient;

        private GlobalService? _globalService;
        private IPBICloudEnvironment? _cloudEnvironment;
        private TenantCluster? _tenantCluster;

        public PBICloudSettingsService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.BaseAddress = PBICloudService.PBIApiUri;
        }

        public IPBICloudEnvironment CloudEnvironment
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
            //_localClientSites = Contracts.PBIDesktop.LocalClientSites.Create();
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
                        _cloudEnvironment = InitializeCloudEnvironment();
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

                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (AppEnvironment.IsDiagnosticLevelVerbose)
                    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudSettingsService) }.{ nameof(InitializeGlobalServiceAsync) }", content);

                var globalService = JsonSerializer.Deserialize<GlobalService>(content, AppEnvironment.DefaultJsonOptions);
                {
                    BravoUnexpectedException.ThrowIfNull(globalService);
                }
                return globalService;
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }
        }

        private IPBICloudEnvironment InitializeCloudEnvironment()
        {
            var pbicloudEnvironmentType = PBICloudEnvironmentType.Public;
            var globalServiceCloudName = pbicloudEnvironmentType.ToGlobalServiceCloudName();
            var globalServiceEnvironment = _globalService?.Environments?.SingleOrDefault((c) => globalServiceCloudName.EqualsI(c.CloudName));

            BravoUnexpectedException.ThrowIfNull(globalServiceEnvironment);

            var pbicloudEnvironment = PBICloudEnvironment.CreateFrom(pbicloudEnvironmentType, globalServiceEnvironment);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudSettingsService) }.{ nameof(InitializeCloudEnvironment) }", content: JsonSerializer.Serialize(pbicloudEnvironment));

            return pbicloudEnvironment;
        }

        private async Task<TenantCluster> GetTenantClusterAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var request = new HttpRequestMessage(HttpMethod.Put, GlobalServiceGetOrInsertClusterUrisByTenantlocationUrl);
            request.Content = new StringContent(string.Empty, Encoding.UTF8, MediaTypeNames.Application.Json);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudSettingsService) }.{ nameof(GetTenantClusterAsync) }", content);

            var tenantCluster = JsonSerializer.Deserialize<TenantCluster>(content, AppEnvironment.DefaultJsonOptions);
            {
                BravoUnexpectedException.ThrowIfNull(tenantCluster);
            }
            return tenantCluster;
        }
    }
}
