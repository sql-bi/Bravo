using Dax.Metadata.Extractor;
using Dax.Vpax.Tools;
using Sqlbi.Bravo.Infrastructure;
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
        Task<IEnumerable<Workspace>> GetWorkspacesAsync(string accessToken);

        Task<IEnumerable<SharedDataset>> GetSharedDatasetsAsync(string accessToken);

        Stream? ExportVpax(PBICloudDataset dataset, string accessToken, bool includeTomModel = true, bool includeVpaModel = true, bool readStatisticsFromData = true, int sampleRows = 0);
    }

    internal class PBICloudService : IPBICloudService, IVpaxExtractor
    {
        private const string WorkspacesUri = "https://api.powerbi.com/powerbi/databases/v201606/workspaces";
        private const string SharedDatasetsUri = "metadata/v201901/gallery/sharedDatasets";

        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = false, // required by SharedDatasetModel LastRefreshTime/lastRefreshTime properties
        };

        public async Task<IEnumerable<Workspace>> GetWorkspacesAsync(string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.GetAsync(WorkspacesUri);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var workspaces = JsonSerializer.Deserialize<IEnumerable<Workspace>>(content, _jsonOptions);

            return workspaces ?? Array.Empty<Workspace>();
        }

        public async Task<IEnumerable<SharedDataset>> GetSharedDatasetsAsync(string accessToken)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://wabi-north-europe-redirect.analysis.windows.net/" /* TenantCluster.FixedClusterUri */ ); //TODO: read tenant cluster config
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.GetAsync(SharedDatasetsUri);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var datasets = JsonSerializer.Deserialize<IEnumerable<SharedDataset>>(content, _jsonOptions);

            return datasets ?? Array.Empty<SharedDataset>();
        }

        public Stream? ExportVpax(PBICloudDataset dataset, string accessToken, bool includeTomModel = true, bool includeVpaModel = true, bool readStatisticsFromData = true, int sampleRows = 0)
        {
            // TODO: set default for readStatisticsFromData and sampleRows arguments

            // PBIDesktop instance is no longer available if parameters cannot be obtained
            var parameters = GetConnectionParameters(dataset, accessToken);
            if (parameters == default)
                return null;

            var daxModel = TomExtractor.GetDaxModel(parameters.ServerName, parameters.DatabaseName, AppConstants.ApplicationName, AppConstants.ApplicationFileVersion, readStatisticsFromData, sampleRows);
            var tomModel = includeTomModel ? TomExtractor.GetDatabase(parameters.ServerName, parameters.DatabaseName) : null;
            var vpaModel = includeVpaModel ? new Dax.ViewVpaExport.Model(daxModel) : null;

            var vpaxPath = Path.GetTempFileName();
            try
            {
                VpaxTools.ExportVpax(vpaxPath, daxModel, vpaModel, tomModel);

                var buffer = File.ReadAllBytes(vpaxPath);
                var vpaxStream = new MemoryStream(buffer, writable: false);

                return vpaxStream;
            }
            finally
            {
                File.Delete(vpaxPath);
            }
        }

        private (string ServerName, string DatabaseName) GetConnectionParameters(PBICloudDataset dataset, string accessToken)
        {
            // Dataset connectivity with the XMLA endpoint
            // https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools

            // Duplicate workspace names
            // https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools#duplicate-workspace-names
            
            // powerbi://api.powerbi.com/v1.0/[tenant name]/[workspace name]
            //var serverName = $"DataSource=powerbi://api.powerbi.com/v1.0/myorg/;Initial Catalog={ dataset.DisplayName };Password={ accessToken }";
            var serverName = $"powerbi://api.powerbi.com/v1.0/myorg/{ dataset.WorkspaceName }";
            var databaseName = $"{ dataset.DisplayName }";

            return (serverName, databaseName);
        }
    }
}
