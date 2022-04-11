namespace Sqlbi.Bravo.Infrastructure.Services.PowerBI
{
    using Microsoft.Win32;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Contracts.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Mime;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;

    public interface IPBICloudSettingsService
    {
        Task<TenantCluster> GetTenantClusterAsync(string accessToken, CancellationToken cancellationToken);

        Task<IEnumerable<IPBICloudEnvironment>> GetEnvironmentsAsync(string userPrincipalName, CancellationToken cancellationToken);
    }

    internal class PBICloudSettingsService : IPBICloudSettingsService
    {
        private const string GlobalServiceEnvironmentsDiscoverUrl = "powerbi/globalservice/v202003/environments/discover?user={0}";
        private const string GlobalServiceGetOrInsertClusterUrisByTenantlocationUrl = "spglobalservice/GetOrInsertClusterUrisByTenantlocation";

        private readonly Lazy<Uri> _environmentDiscoverBaseUri;
        private readonly HttpClient _httpClient;

        public PBICloudSettingsService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _environmentDiscoverBaseUri = new(() => GetEnvironmentDiscoverBaseUri());
        }

        public async Task<TenantCluster> GetTenantClusterAsync(string accessToken, CancellationToken cancellationToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var requestUri = new Uri(_environmentDiscoverBaseUri.Value, relativeUri: GlobalServiceGetOrInsertClusterUrisByTenantlocationUrl);
            using var request = new HttpRequestMessage(HttpMethod.Put, requestUri);
            request.Content = new StringContent(string.Empty, Encoding.UTF8, MediaTypeNames.Application.Json);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudSettingsService) }.{ nameof(GetTenantClusterAsync) }", content);

            var tenantCluster = JsonSerializer.Deserialize<TenantCluster>(content, AppEnvironment.DefaultJsonOptions);
            BravoUnexpectedException.ThrowIfNull(tenantCluster);
            
            return tenantCluster;
        }

        public async Task<IEnumerable<IPBICloudEnvironment>> GetEnvironmentsAsync(string userPrincipalName, CancellationToken cancellationToken)
        {
            var relativeUri = GlobalServiceEnvironmentsDiscoverUrl.FormatInvariant(HttpUtility.UrlEncode(userPrincipalName));
            var requestUri = new Uri(_environmentDiscoverBaseUri.Value, relativeUri);

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Array.Empty<PBICloudEnvironment>();
            }
            else
            {
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (AppEnvironment.IsDiagnosticLevelVerbose)
                    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudSettingsService) }.{ nameof(GetEnvironmentsAsync) }", content);

                var globalService = JsonSerializer.Deserialize<GlobalService>(content, AppEnvironment.DefaultJsonOptions);
                var environments = globalService?.Environments?.Select(PBICloudEnvironment.CreateFrom).ToArray();

                return environments ?? Array.Empty<PBICloudEnvironment>();
            }
        }

        private static Uri GetEnvironmentDiscoverBaseUri()
        {
            const string PowerBIDiscoveryUrl = "PowerBIDiscoveryUrl";

            // Uri strings from endpoint query > globalservice/v202003/environments/discover?client=powerbi-msolap
            string[] TrustedDiscoverUriString = new[]
            {
                PBICloudService.PBCommercialUri,                               // PBICloudEnvironmentType.Public
                "https://api.powerbi.cn",                                      // PBICloudEnvironmentType.China
                "https://api.powerbi.de",                                      // PBICloudEnvironmentType.Germany
                "https://api.powerbigov.us",                                   // PBICloudEnvironmentType.USGov
                "https://api.high.powerbigov.us",                              // PBICloudEnvironmentType.USGovHigh
                "https://api.mil.powerbigov.us",                               // PBICloudEnvironmentType.USGovMil
                // "https://biazure-int-edog-redirect.analysis-df.windows.net", // disabled PpeCloud
                // "https://api.powerbi.eaglex.ic.gov",                         // disabled USNatCloud
                // "https://api.powerbi.microsoft.scloud",                      // disabled USSecCloud
            };

            // https://docs.microsoft.com/en-us/power-bi/enterprise/service-govus-overview#sign-in-to-power-bi-for-us-government
            // https://github.com/microsoft/Federal-Business-Applications/tree/main/whitepapers/power-bi-registry-settings

            var uriString = CommonHelper.ReadRegistryString(Registry.LocalMachine, keyName: "SOFTWARE\\Microsoft\\Microsoft Power BI", valueName: PowerBIDiscoveryUrl);

            if (uriString is null)
                uriString = CommonHelper.ReadRegistryString(Registry.LocalMachine, keyName: "SOFTWARE\\WOW6432Node\\Policies\\Microsoft\\Microsoft Power BI", valueName: PowerBIDiscoveryUrl);

            if (uriString is not null)
            {
                if (TrustedDiscoverUriString.Contains(uriString, StringComparer.OrdinalIgnoreCase) == false)
                {
                    throw new BravoUnexpectedInvalidOperationException($"Untrusted { nameof(PowerBIDiscoveryUrl) } value ({ uriString })");
                }
            }
            else
            {
                uriString = PBICloudService.PBCommercialUri;
            }

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Text, name: $"{ nameof(PBICloudSettingsService) }.{ nameof(GetEnvironmentDiscoverBaseUri) }", content: uriString);

            var baseUri = new Uri(uriString);
            return baseUri;
        }
    }
}
