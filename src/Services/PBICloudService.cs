using Microsoft.Identity.Client;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
using Sqlbi.Bravo.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Services
{
    public interface IPBICloudService
    {
        BravoAccount? CurrentAccount { get; }

        Task<bool> IsSignInRequiredAsync();

        Task SignInAsync(string? userPrincipalName = null);
        
        Task SignOutAsync();

        Task<IEnumerable<Workspace>> GetWorkspacesAsync();

        Task<IEnumerable<SharedDataset>> GetSharedDatasetsAsync();

        Stream ExportVpax(PBICloudDataset dataset, bool includeTomModel = true, bool includeVpaModel = true, bool readStatisticsFromData = true, int sampleRows = 0);

        void Update(PBICloudDataset dataset, IEnumerable<FormattedMeasure> measures);
     }

    internal class PBICloudService : IPBICloudService
    {
        private const string GetWorkspacesRequestUri = "powerbi/databases/v201606/workspaces";
        private const string GetGallerySharedDatasetsRequestUri = "metadata/v201901/gallery/sharedDatasets";
        private const string GetResourceUserPhotoRequestUri = "powerbi/version/201606/resource/userPhoto/?userId={0}";

        private readonly HttpClient _httpClient;
        private readonly IPBICloudAuthenticationService _authenticationService;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = false, // required by SharedDatasetModel LastRefreshTime/lastRefreshTime properties
        };

        public PBICloudService(IPBICloudAuthenticationService authenticationService, HttpClient httpClient)
        {
            _authenticationService = authenticationService;
            _httpClient = httpClient;
        }

        private AuthenticationResult? CurrentAuthentication => _authenticationService.Authentication;

        public BravoAccount? CurrentAccount { get; private set; }

        public async Task<bool> IsSignInRequiredAsync()
        {
            var refreshSucceeded = await _authenticationService.RefreshTokenAsync().ConfigureAwait(false);
            if (refreshSucceeded)
            {
                await RefreshCurrentAccountAsync().ConfigureAwait(false);
                // No SignIn required - cached token is valid
                return false;
            }
            else
            {
                // SignIn required - an interaction is required with the end user of the application, for instance:
                // - no refresh token was in the cache
                // - the user needs to consent or re-sign-in (for instance if the password expired)
                // - the user needs to perform two factor auth
                return true;
            }
        }

        public async Task SignInAsync(string? loginHint = null)
        {
            CurrentAccount = null;

            try
            {
                await _authenticationService.AcquireTokenAsync(silent: false, loginHint, timeout: AppConstants.MSALSignInTimeout).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new SignInException(BravoProblem.SignInMsalTimeoutExpired);
            }
            catch (MsalException mex)
            {
                throw new SignInException(BravoProblem.SignInMsalExceptionOccurred, mex.ErrorCode, mex);
            }

           await RefreshCurrentAccountAsync().ConfigureAwait(false);
        }

        public async Task SignOutAsync()
        {
            CurrentAccount = null;

            await _authenticationService.ClearTokenCacheAsync().ConfigureAwait(false);

            if (CurrentAuthentication is not null)
            {
                throw new BravoUnexpectedException("CurrentAuthentication is not null");
            }
        }

        public async Task<IEnumerable<Workspace>> GetWorkspacesAsync()
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(CurrentAuthentication?.CreateAuthorizationHeader());

            var requestUri = new Uri(AppConstants.PBICloudApiUri, relativeUri: GetWorkspacesRequestUri);
            using var response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var workspaces = JsonSerializer.Deserialize<IEnumerable<Workspace>>(content, _jsonOptions);

            return workspaces ?? Array.Empty<Workspace>();
        }

        public async Task<IEnumerable<SharedDataset>> GetSharedDatasetsAsync()
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(CurrentAuthentication?.CreateAuthorizationHeader());

            var requestUri = new Uri(_authenticationService.TenantCluster, relativeUri: GetGallerySharedDatasetsRequestUri);
            using var response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var datasets = JsonSerializer.Deserialize<IEnumerable<SharedDataset>>(content, _jsonOptions);

            return datasets ?? Array.Empty<SharedDataset>();
        }

        public Stream ExportVpax(PBICloudDataset dataset, bool includeTomModel, bool includeVpaModel, bool readStatisticsFromData, int sampleRows)
        {
            var (connectionString, databaseName) = GetConnectionParameters(dataset);
            var stream = VpaxToolsHelper.ExportVpax(connectionString, databaseName, includeTomModel, includeVpaModel, readStatisticsFromData, sampleRows);

            return stream;
        }

        public void Update(PBICloudDataset dataset, IEnumerable<FormattedMeasure> measures)
        {
            var (connectionString, databaseName) = GetConnectionParameters(dataset);
            
            TabularModelHelper.Update(connectionString, databaseName, measures);
        }

        /// <summary>
        /// Build the PBICloudDataset connection string and database name
        /// </summary>
        private (string ConnectionString, string DatabaseName) GetConnectionParameters(PBICloudDataset dataset)
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

        private async Task<string?> GetAccountAvatarAsync(string username)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(CurrentAuthentication?.CreateAuthorizationHeader());

            var requestUri = new Uri(AppConstants.PBICloudApiUri, relativeUri: GetResourceUserPhotoRequestUri.FormatInvariant(username));
            using var response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var bitmap = new Bitmap(stream);
                using var memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, bitmap.RawFormat);

                var imageBase64String = Convert.ToBase64String(memoryStream.ToArray());
                var imageMimeType = GetMimeType(bitmap);

                var encodedImage = string.Format(CultureInfo.InvariantCulture, "data:{0};base64,{1}", imageMimeType, imageBase64String);
                return encodedImage;
            }

            //var cachedImage = _authenticationService.CachedUserInfo?.Avatar;
            //return cachedImage;

            return null;

            static string? GetMimeType(Bitmap bitmap) => ImageCodecInfo.GetImageDecoders().FirstOrDefault((c) => c.FormatID == bitmap.RawFormat.Guid)?.MimeType;
        }

        private async Task RefreshCurrentAccountAsync()
        {
            var currentAuthentication = CurrentAuthentication ?? throw new BravoUnexpectedException("CurrentAuthentication is null");
            var currentAccountChanged = currentAuthentication.Account.HomeAccountId.Identifier.Equals(CurrentAccount?.Identifier) == false;
            if (currentAccountChanged)
            {
                CurrentAccount = new BravoAccount
                {
                    Identifier = currentAuthentication.Account.HomeAccountId.Identifier,
                    UserPrincipalName = currentAuthentication.Account.Username,
                    Username = currentAuthentication.ClaimsPrincipal.FindFirst((c) => c.Type == "name")?.Value,
                    Avatar = await GetAccountAvatarAsync(currentAuthentication.Account.Username).ConfigureAwait(false),
                };
            }
        }
    }
}
