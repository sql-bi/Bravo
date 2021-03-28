using Microsoft.Identity.Client;
using Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models;
using Sqlbi.Bravo.Core;
using Sqlbi.Bravo.Core.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Client.PowerBI.PowerBICloud
{
    internal static class PowerBICloudManager
    {
        private const string DiscoverEnvironmentsUrl = "https://api.powerbi.com/powerbi/globalservice/v201606/environments/discover?client=powerbi-msolap";
        private const string GlobalServiceGetClusterUrl = "https://api.powerbi.com/spglobalservice/GetOrInsertClusterUrisByTenantlocation";
        private const string GetSharedDatasetsUrl = "metadata/v201901/gallery/sharedDatasets";
        private const string CloudEnvironmentGlobalCloudName = "GlobalCloud";
        private const string MicrosoftAccountOnlyQueryParameter = "msafed=0";

        private static readonly object _tokenCacheLock = new object();

        private static IPublicClientApplication PublicClientApplication;
        private static PowerBICloudEnvironment CloudEnvironment;
        private static GlobalService GlobalService;
        private static TenantCluster TenantCluster;

        private static async Task InitializeCloudSettingsAsync()
        {
            await InitializeGlobalServiceAsync();

            await InitializeCloudEnvironmentAsync(CloudEnvironmentGlobalCloudName);

            static async Task InitializeGlobalServiceAsync()
            {
                if (GlobalService == null)
                {
                    var securityProtocol = ServicePointManager.SecurityProtocol;
                    try
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.Accept.Clear();

                        var response = await client.PostAsync(requestUri: DiscoverEnvironmentsUrl, content: null);
                        response.EnsureSuccessStatusCode();

                        var json = await response.Content.ReadAsStringAsync();
                        GlobalService = JsonSerializer.Deserialize<GlobalService>(json, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));                        
                    }
                    finally
                    {
                        ServicePointManager.SecurityProtocol = securityProtocol;
                    }
                }
            }

            static async Task InitializeCloudEnvironmentAsync(string environmentName)
            {
                if (CloudEnvironment == null)
                {
                    var globalEnvironment = GlobalService.Environments.SingleOrDefault((c) => environmentName.Equals(c.CloudName, StringComparison.OrdinalIgnoreCase));
                    if (globalEnvironment == null)
                        throw new NotSupportedException($"Cloud environment not found [{ environmentName }]");

                    var service = globalEnvironment.Services.Single((s) => "powerbi-backend".Equals(s.Name, StringComparison.OrdinalIgnoreCase));
                    var client = globalEnvironment.Clients.Single((c) => "powerbi-gateway".Equals(c.Name, StringComparison.OrdinalIgnoreCase));

                    CloudEnvironment = new PowerBICloudEnvironment
                    {
                        Name = PowerBICloudEnvironmentType.Public,
                        AuthorityUri = globalEnvironment.Services.Single((s) => "aad".Equals(s.Name, StringComparison.OrdinalIgnoreCase)).Endpoint,
                        RedirectUri = client.RedirectUri,
                        ClientId = client.AppId,
                        Scopes = new string[] { $"{ service.ResourceId }/.default" }
                    };
                }

                await Task.CompletedTask;
            }
        }

        private static async Task InitializeTenantClusterAsync(string accessToken)
        {
            if (TenantCluster == null)
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
                using var response = await client.PutAsync(GlobalServiceGetClusterUrl, content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                TenantCluster = JsonSerializer.Deserialize<TenantCluster>(json, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
        }

        public static async Task<AuthenticationResult> AcquireTokenAsync(IAccount account, CancellationToken cancellationToken)
        {
            await InitializeCloudSettingsAsync();

            if (PublicClientApplication == null)
            {
                PublicClientApplication = PublicClientApplicationBuilder.Create(CloudEnvironment.ClientId)
                    .WithAuthority(CloudEnvironment.AuthorityUri)
                    .WithDefaultRedirectUri()
                    .Build();

                //var tokenCache = PublicClientApplication.UserTokenCache;
                //tokenCache.SetBeforeAccess(TokenCacheBeforeAccessCallback);
                //tokenCache.SetAfterAccess(TokenCacheAfterAccessCallback);
            }

            AuthenticationResult authenticationResult;

            if (account != null)
            {
                authenticationResult = await PublicClientApplication.AcquireTokenSilent(CloudEnvironment.Scopes, account)
                    .WithExtraQueryParameters(MicrosoftAccountOnlyQueryParameter)
                    .ExecuteAsync(cancellationToken);

                return authenticationResult;
            }

            authenticationResult = await PublicClientApplication.AcquireTokenInteractive(CloudEnvironment.Scopes)
                .WithExtraQueryParameters(MicrosoftAccountOnlyQueryParameter)
                .ExecuteAsync(cancellationToken);

            await InitializeTenantClusterAsync(authenticationResult.AccessToken);

            return authenticationResult;
        }

        public static async Task RemoveTokenAsync()
        {
            var accounts = await PublicClientApplication.GetAccountsAsync();

            foreach (var account in accounts)
            {
                await PublicClientApplication.RemoveAsync(account);
            }
        }

        public static void TokenCacheBeforeAccessCallback(TokenCacheNotificationArgs args)
        {
            lock (_tokenCacheLock)
            {
                byte[] msalV3State = null;

                if (File.Exists(AppConstants.PowerBICloudTokenCacheFile))
                {
                    var encryptedData = File.ReadAllBytes(AppConstants.PowerBICloudTokenCacheFile);
                    msalV3State = DataProtection.Unprotect(encryptedData);
                }

                args.TokenCache.DeserializeMsalV3(msalV3State);
            }
        }

        public static void TokenCacheAfterAccessCallback(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                lock (_tokenCacheLock)
                {
                    var msalV3State = args.TokenCache.SerializeMsalV3();
                    var encryptedData = DataProtection.Protect(msalV3State);

                    File.WriteAllBytes(AppConstants.PowerBICloudTokenCacheFile, encryptedData);
                }
            }
        }

        public static async Task<IEnumerable<MetadataSharedDataset>> GetSharedDatasetsAsync(string accessToken)
        {
            using var client = new HttpClient();
            {
                client.BaseAddress = new Uri(TenantCluster.FixedClusterUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            using var response = await client.GetAsync(GetSharedDatasetsUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var datasets = JsonSerializer.Deserialize<IEnumerable<MetadataSharedDataset>>(json, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return datasets;
        }
    }
}