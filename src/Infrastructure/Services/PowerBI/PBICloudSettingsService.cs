﻿namespace Sqlbi.Bravo.Infrastructure.Services.PowerBI
{
    using Microsoft.Win32;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Contracts.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Extensions;
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
        Task<TenantCluster> GetTenantClusterAsync(IPBICloudEnvironment environment, string accessToken, CancellationToken cancellationToken);

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
            _environmentDiscoverBaseUri = new(() => GetEnvironmentDiscoveryBaseUri());
        }

        public async Task<TenantCluster> GetTenantClusterAsync(IPBICloudEnvironment environment, string accessToken, CancellationToken cancellationToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var requestUri = environment.GetServiceEndpointUri(GlobalServiceGetOrInsertClusterUrisByTenantlocationUrl);
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
                var environments = globalService?.Environments?.Select(PBICloudEnvironment.CreateFrom).Where((e) => !e.IsMicrosoftInternal).ToArray();
                
                return environments ?? Array.Empty<PBICloudEnvironment>();
            }
        }

        private static Uri GetEnvironmentDiscoveryBaseUri()
        {
            const string PowerBIDiscoveryUrl = "PowerBIDiscoveryUrl";

            // https://docs.microsoft.com/en-us/power-bi/enterprise/service-govus-overview#sign-in-to-power-bi-for-us-government
            // https://github.com/microsoft/Federal-Business-Applications/tree/main/whitepapers/power-bi-registry-settings

            var uriString = Registry.LocalMachine.GetStringValue(subkeyName: "SOFTWARE\\Microsoft\\Microsoft Power BI", valueName: PowerBIDiscoveryUrl);

            if (uriString is null)
                uriString = Registry.LocalMachine.GetStringValue(subkeyName: "SOFTWARE\\WOW6432Node\\Policies\\Microsoft\\Microsoft Power BI", valueName: PowerBIDiscoveryUrl);

            if (uriString is null)
                uriString = PBICloudEnvironmentTypeExtensions.GlobalCloudApiUri;
 
            if (Uri.TryCreate(uriString, UriKind.Absolute, out var discoveryUri) == false || PBICloudEnvironmentTypeExtensions.TrustedApiUris.Contains(discoveryUri) == false)
            {
                throw new BravoUnexpectedInvalidOperationException($"Unsupported Power BI environment discovery URL ({ uriString })");
            }

            var baseUri = new Uri(uriString, UriKind.Absolute);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Text, name: $"{ nameof(PBICloudSettingsService) }.{ nameof(GetEnvironmentDiscoveryBaseUri) }", content: baseUri.AbsoluteUri);

            return baseUri;
        }
    }
}
