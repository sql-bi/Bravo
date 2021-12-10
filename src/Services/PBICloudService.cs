using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Services
{
    internal class PBICloudService : IPBICloudService
    {
        private const string WorkspacesUri = "https://api.powerbi.com/powerbi/databases/v201606/workspaces";
        private const string SharedDatasetsUri = "metadata/v201901/gallery/sharedDatasets";

        private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

        public async Task<IEnumerable<Workspace>> GetWorkspacesAsync(string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.GetAsync(WorkspacesUri);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var workspaces = JsonSerializer.Deserialize<IEnumerable<Workspace>>(content, _serializerOptions);

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

            var json = await response.Content.ReadAsStringAsync();
            var datasets = JsonSerializer.Deserialize<IEnumerable<SharedDataset>>(json, _serializerOptions);

            return datasets ?? Array.Empty<SharedDataset>();
        }
    }
}
