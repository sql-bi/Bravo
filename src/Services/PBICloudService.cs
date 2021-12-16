using Microsoft.Identity.Client;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
using Sqlbi.Bravo.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Services
{
    public interface IPBICloudService
    {
        bool IsAuthenticated { get; }

        BravoAccount? CurrentAccount { get; }

        Task SignInAsync();
        
        Task SignOutAsync();

        Task<IEnumerable<Workspace>> GetWorkspacesAsync();

        Task<IEnumerable<SharedDataset>> GetSharedDatasetsAsync();

        Stream ExportVpax(PBICloudDataset dataset, bool includeTomModel = true, bool includeVpaModel = true, bool readStatisticsFromData = true, int sampleRows = 0);

        void Update(PBICloudDataset dataset, IEnumerable<FormattedMeasure> measures);
     }

    internal class PBICloudService : IPBICloudService
    {
        private const string GetWorkspacesRequestUri = "https://api.powerbi.com/powerbi/databases/v201606/workspaces";
        private const string GetGallerySharedDatasetsRequestUri = "metadata/v201901/gallery/sharedDatasets";

        private readonly IPBICloudAuthenticationService _authenticationService;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = false, // required by SharedDatasetModel LastRefreshTime/lastRefreshTime properties
        };

        public PBICloudService(IPBICloudAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        private AuthenticationResult? CurrentAuthentication => _authenticationService.Authentication;

        public BravoAccount? CurrentAccount { get; private set; }

        public bool IsAuthenticated => CurrentAccount is not null && _authenticationService.Authentication?.ClaimsPrincipal?.Identity is not null;

        public async Task SignInAsync()
        {
            try
            {
                await _authenticationService.AcquireTokenAsync(cancelAfter: AppConstants.MSALSignInTimeout).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new SignInTimeoutException();
            }
            catch (MsalException mex)
            {
                throw new SignInMsalException(mex);
            }

            CurrentAccount = new BravoAccount
            {
                Identifier = CurrentAuthentication!.Account.HomeAccountId.Identifier,
                UserPrincipalName = CurrentAuthentication!.Account.Username,
                Username = CurrentAuthentication!.ClaimsPrincipal.FindFirst((c) => c.Type == "name")?.Value,
            };
        }

        public async Task SignOutAsync()
        {
            CurrentAccount = null;

            await _authenticationService.ClearTokenCacheAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<Workspace>> GetWorkspacesAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CurrentAuthentication?.AccessToken);

            using var response = await client.GetAsync(GetWorkspacesRequestUri).ConfigureAwait(false);

            var content = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().ConfigureAwait(false);
            var workspaces = JsonSerializer.Deserialize<IEnumerable<Workspace>>(content, _jsonOptions);

            return workspaces ?? Array.Empty<Workspace>();
        }

        public async Task<IEnumerable<SharedDataset>> GetSharedDatasetsAsync()
        {
            var clusterUri = _authenticationService.CloudSettings.TenantCluster?.FixedClusterUri ?? throw new BravoException("PBICloud shared datasets null tenant cluster");

            using var client = new HttpClient();
            client.BaseAddress = new Uri(clusterUri);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CurrentAuthentication?.AccessToken);

            using var response = await client.GetAsync(GetGallerySharedDatasetsRequestUri).ConfigureAwait(false);

            var content = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().ConfigureAwait(false);
            var datasets = JsonSerializer.Deserialize<IEnumerable<SharedDataset>>(content, _jsonOptions);

            return datasets ?? Array.Empty<SharedDataset>();
        }

        public Stream ExportVpax(PBICloudDataset dataset, bool includeTomModel, bool includeVpaModel, bool readStatisticsFromData, int sampleRows)
        {
            var (connectionString, databaseName) = GetConnectionParameters(dataset);

            try
            {
                var stream = VpaxToolsHelper.ExportVpax(connectionString, databaseName, includeTomModel, includeVpaModel, readStatisticsFromData, sampleRows);
                return stream;
            }
            catch (ArgumentException ex) when (ex.Message == $"The database '{ databaseName }' could not be found. Either it does not exist or you do not have admin rights to it.")
            {
                // TODO: do not use message string to identify the error
                throw new TOMDatabaseNotFoundException("PBICloud dataset not found");
            }
        }

        public void Update(PBICloudDataset dataset, IEnumerable<FormattedMeasure> measures)
        {
            var (connectionString, databaseName) = GetConnectionParameters(dataset);
            
            TabularModelHelper.Update(connectionString, databaseName, measures);
        }

        private (string connectionString, string databaseName) GetConnectionParameters(PBICloudDataset dataset)
        {
            // Dataset connectivity with the XMLA endpoint
            // https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools

            // Duplicate workspace names
            // https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools#duplicate-workspace-names

            // Connection properties
            // https://docs.microsoft.com/en-us/analysis-services/instances/connection-string-properties-analysis-services?view=asallproducts-allversions

            var tenantName = "myorg"; // TODO: add support for B2B users tenant name
            var serverName = $"powerbi://api.powerbi.com/v1.0/{ tenantName }/{ dataset.WorkspaceName }";
            var databaseName = dataset.DisplayName;

            var connectionString = ConnectionStringHelper.BuildForPBICloudDataset(serverName, databaseName, CurrentAuthentication?.AccessToken);

            return (connectionString, databaseName);
        }
    }
}
