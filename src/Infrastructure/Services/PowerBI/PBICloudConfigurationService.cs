namespace Sqlbi.Bravo.Infrastructure.Services.PowerBI
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Contracts;
    using Sqlbi.Bravo.Infrastructure.Contracts.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Serialization;
    using Sqlbi.Bravo.Models;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web;

    internal interface IPBICloudConfigurationService
    {
        Task<IEnumerable<CloudEnvironment>> DiscoverCloudEnvironmentsAsync(string email, CancellationToken cancellationToken);
        Task<string> ResolveTenantClusterUriAsync(CloudEnvironment environment, string accessToken, CancellationToken cancellationToken);
    }

    internal sealed class PBICloudConfigurationService : IPBICloudConfigurationService
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _discoveryBaseUri;

        public PBICloudConfigurationService(IPBILocalConfigurationReader pbiLocalConfiguration)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));

            _discoveryBaseUri = pbiLocalConfiguration.GetPowerBIServiceDiscoveryBaseUri()
                ?? PBIConstants.Endpoints.GlobalCloudPowerBIUri;
        }

        public async Task<IEnumerable<CloudEnvironment>> DiscoverCloudEnvironmentsAsync(string email, CancellationToken cancellationToken)
        {
            var response = await DiscoverCloudEnvironmentsAsync(email, apiVersion: "v202408", cancellationToken);
            if (response is null)
                response = await DiscoverCloudEnvironmentsAsync(email, apiVersion: "v202003", cancellationToken);

            var environments = response?.Environments ?? [];

            return [.. environments
                .Where((e) => !e.IsMicrosoftInternalCloud()) // Filter out Microsoft internal environments
                .Select(CloudEnvironment.FromContract)];
        }

        public async Task<string> ResolveTenantClusterUriAsync(CloudEnvironment environment, string accessToken, CancellationToken cancellationToken)
        {
            var requestUri = environment.GetBackendRequestUri("spglobalservice/GetOrInsertClusterUrisByTenantlocation");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Put, requestUri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpRequest.Content = new StringContent(string.Empty, Encoding.UTF8, MediaTypeNames.Application.Json);

            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            var json = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{nameof(PBICloudConfigurationService)}.{nameof(ResolveTenantClusterUriAsync)}", json);

            var tenantCluster = PBIServiceJsonSerializer.Deserialize<TenantClusterContract>(json);
            return tenantCluster.FixedClusterUri;
        }

        private async Task<CloudEnvironmentResponseContract?> DiscoverCloudEnvironmentsAsync(string email, string apiVersion, CancellationToken cancellationToken)
        {
            var relativeUri = FormattableString.Invariant($"powerbi/globalservice/{apiVersion}/environments/discover?user={HttpUtility.UrlEncode(email)}");
            var requestUri = new Uri(_discoveryBaseUri, relativeUri);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                var json = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

                if (AppEnvironment.IsDiagnosticLevelVerbose)
                    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{nameof(PBICloudConfigurationService)}.{nameof(DiscoverCloudEnvironmentsAsync)}()", content: json);

                return PBIServiceJsonSerializer.Deserialize<CloudEnvironmentResponseContract>(json);
            }
            else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            throw new HttpRequestException($"Unexpected response status code {(int)httpResponse.StatusCode} ({httpResponse.ReasonPhrase}) from environment discovery.", inner: null, httpResponse.StatusCode);
        }
    }
}
