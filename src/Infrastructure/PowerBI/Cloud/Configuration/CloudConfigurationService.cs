namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Configuration
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.PowerBI;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Contracts;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Serialization;
    using Sqlbi.Bravo.Models;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web;

    internal interface ICloudConfigurationService
    {
        Task<IReadOnlyList<CloudEnvironment>> DiscoverEnvironmentsAsync(string email, CancellationToken cancellationToken);

        Task<string> ResolveTenantClusterUriAsync(CloudEnvironment environment, string accessToken, CancellationToken cancellationToken);
    }

    internal sealed class CloudConfigurationService : ICloudConfigurationService
    {
        // The Global Cloud is the default discovery base URI for Power BI service discovery.
        private static readonly Uri s_defaultDiscoveryBaseUri = new("https://api.powerbi.com", UriKind.Absolute);

        private readonly HttpClient _httpClient;
        private readonly Uri _discoveryBaseUri;

        public CloudConfigurationService(IHttpClientFactory httpClientFactory, ILocalConfigurationReader localConfigurationReader)
        {
            _httpClient = httpClientFactory.CreateClient(ServiceCollectionExtensions.PowerBIApiHttpClientName);

            _discoveryBaseUri = localConfigurationReader.GetPowerBIServiceDiscoveryBaseUri()
                ?? s_defaultDiscoveryBaseUri;
        }

        public async Task<IReadOnlyList<CloudEnvironment>> DiscoverEnvironmentsAsync(string email, CancellationToken cancellationToken)
        {
            var response = await DiscoverEnvironmentsAsync(email, apiVersion: "v202408", cancellationToken);
            if (response is null)
                response = await DiscoverEnvironmentsAsync(email, apiVersion: "v202003", cancellationToken);

            var environments = response?.Environments ?? [];

            return [.. environments
                .Where((e) => !e.IsMicrosoftInternalCloud()) // Filter out Microsoft internal environments
                .Select(CloudEnvironment.FromContract)];
        }

        public async Task<string> ResolveTenantClusterUriAsync(CloudEnvironment environment, string accessToken, CancellationToken cancellationToken)
        {
            var relativeUri = "spglobalservice/GetOrInsertClusterUrisByTenantlocation";
            var requestUri = environment.GetBackendRequestUri(relativeUri);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Put, requestUri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpRequest.Content = new StringContent(string.Empty, Encoding.UTF8, MediaTypeNames.Application.Json);

            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            var json = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{nameof(CloudConfigurationService)}.{nameof(ResolveTenantClusterUriAsync)}", json);

            var tenantCluster = CloudContractJsonSerializer.Deserialize<TenantClusterContract>(json);
            return tenantCluster.FixedClusterUri;
        }

        private async Task<CloudEnvironmentResponseContract?> DiscoverEnvironmentsAsync(string email, string apiVersion, CancellationToken cancellationToken)
        {
            var relativeUri = "powerbi/globalservice/{0}/environments/discover?user={1}".FormatInvariant(apiVersion, HttpUtility.UrlEncode(email));
            var requestUri = new Uri(_discoveryBaseUri, relativeUri);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                var json = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

                if (AppEnvironment.IsDiagnosticLevelVerbose)
                    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{nameof(CloudConfigurationService)}.{nameof(DiscoverEnvironmentsAsync)}()", content: json);

                return CloudContractJsonSerializer.Deserialize<CloudEnvironmentResponseContract>(json);
            }
            else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            throw new HttpRequestException($"Unexpected response status code {(int)httpResponse.StatusCode} ({httpResponse.ReasonPhrase}) from environment discovery.", inner: null, httpResponse.StatusCode);
        }
    }
}
