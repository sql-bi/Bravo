namespace Sqlbi.Bravo.Services
{
    using Microsoft.Identity.Client;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading.Tasks;

    public interface IPBICloudService
    {
        BravoAccount? CurrentAccount { get; }

        string? CurrentAccessToken { get; }

        Task<bool> IsSignInRequiredAsync();

        Task SignInAsync(string? userPrincipalName = null);
        
        Task SignOutAsync();

        Task<IEnumerable<PBICloudDataset>> GetDatasetsAsync();

        Stream ExportVpax(PBICloudDataset dataset, bool includeTomModel = true, bool includeVpaModel = true, bool readStatisticsFromData = true, int sampleRows = 0);

        string Update(PBICloudDataset dataset, IEnumerable<FormattedMeasure> measures);

        Task<string?> GetAccountAvatarAsync();
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

        public static readonly Uri PBIApiUri = new("https://api.powerbi.com");
        public static readonly Uri PBIPremiumServerUri = new($"{ PBIPremiumProtocolScheme }://api.powerbi.com");

        /// <summary>
        /// All Power BI workspaces published to the Power BI service
        /// </summary>
        public const string PBIDatasetProtocolScheme = "pbiazure";
        /// <summary>
        /// All Power BI workspaces published to the Power BI service and assigned to Premium Capacity (i.e. workspaces assigned to a Px, Ax or EMx SKU), or Premium-Per-User (PPU)
        /// </summary>
        public const string PBIPremiumProtocolScheme = "powerbi";
        //public const string PBIDedicatedProtocolScheme = "pbidedicated";
        //public const string ASAzureLinkProtocolScheme = "link";
        //public const string ASAzureProtocolScheme = "asazure";

        public PBICloudService(IPBICloudAuthenticationService authenticationService, HttpClient httpClient)
        {
            _authenticationService = authenticationService;
            _httpClient = httpClient;
        }

        private AuthenticationResult? CurrentAuthentication => _authenticationService.Authentication;

        public string? CurrentAccessToken => _authenticationService.Authentication?.AccessToken;

        public BravoAccount? CurrentAccount { get; private set; }

        public async Task<bool> IsSignInRequiredAsync()
        {
            var refreshSucceeded = await _authenticationService.RefreshTokenAsync().ConfigureAwait(false);
            if (refreshSucceeded)
            {
                RefreshCurrentAccount();
                return false;  // No SignIn required - cached token is valid
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
                await _authenticationService.AcquireTokenAsync(silent: false, loginHint, timeout: AppEnvironment.MSALSignInTimeout).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new SignInException(BravoProblem.SignInMsalTimeoutExpired);
            }
            catch (MsalException mex)
            {
                throw new SignInException(BravoProblem.SignInMsalExceptionOccurred, mex.ErrorCode, mex);
            }

           RefreshCurrentAccount();
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

        public async Task<IEnumerable<PBICloudDataset>> GetDatasetsAsync()
        {
            var onlineWorkspaces = await GetWorkspacesAsync();
            var onlineDatasets = await GetSharedDatasetsAsync();

            /*
             * SELECT datasets d LEFT OUTER JOIN workspaces w ON d.WorkspaceObjectId = w.Id
             * Here we use a LEFT OUTER JOIN in order to include the user's personal workspace datasets.
             */
            var datasets = from od in onlineDatasets
                           join ow in onlineWorkspaces on od.WorkspaceObjectId?.ToLowerInvariant() equals ow.WorkspaceObjectId?.ToLowerInvariant() into joinedWorkspaces
                           from jw in joinedWorkspaces.DefaultIfEmpty()
                           select new PBICloudDataset
                           {
                               WorkspaceId = jw?.Id,
                               WorkspaceName = jw?.Name ?? GetWorkspaceName(od),
                               WorkspaceObjectId = jw?.WorkspaceObjectId,
                               Id = od.Model?.Id,
                               ServerName = PBIPremiumServerUri.OriginalString,
                               DatabaseName = od.Model?.DBName,
                               DisplayName = od.Model?.DisplayName,
                               Description = od.Model?.Description,
                               Owner = $"{ od.Model?.CreatorUser?.GivenName } { od.Model?.CreatorUser?.FamilyName }",
                               Refreshed = od.Model?.LastRefreshTime,
                               Endorsement = (PBICloudDatasetEndorsement)(od.GalleryItem?.Stage ?? (int)PBICloudDatasetEndorsement.None),
                               ConnectionMode = GetConnectionMode(jw, od),
                               Diagnostic = GetDiagnostic(jw, od)
                           };

            //File.WriteAllText(Path.Combine(AppEnvironment.ApplicationTempPath, "onlineWorkspaces.json"), JsonSerializer.Serialize(onlineWorkspaces));
            //File.WriteAllText(Path.Combine(AppEnvironment.ApplicationTempPath, "onlineDatasets.json"), JsonSerializer.Serialize(onlineDatasets));
            //File.WriteAllText(Path.Combine(AppEnvironment.ApplicationTempPath, "datasets.json"), JsonSerializer.Serialize(datasets));

            return datasets.ToArray();

            string? GetWorkspaceName(CloudSharedModel dataset)
            {
                if (dataset.IsOnPersonalWorkspace)
                    return CurrentAccount?.UserPrincipalName;

                return null;
            }

            static PBICloudDatasetConnectionMode GetConnectionMode(CloudWorkspace? workspace, CloudSharedModel dataset)
            {
                if (workspace is null || workspace.IsPersonalWorkspace || dataset.IsOnPersonalWorkspace)
                    return PBICloudDatasetConnectionMode.UnsupportedPersonalWorkspace;

                if (workspace.IsXmlaEndPointSupported == false)
                    return PBICloudDatasetConnectionMode.UnsupportedWorkspaceSku;

                if (dataset.Model is not null)
                {
                    // Exclude unsupported datasets - a.k.a. datasets not accessible by the XMLA endpoint
                    // see https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools#unsupported-datasets

                    if (dataset.Model.IsOnPremModel)
                        return PBICloudDatasetConnectionMode.UnsupportedOnPremLiveConnection;

                    // TODO: Exclude datasets based on a live connection to a Power BI dataset in another workspace
                    //if ( ?? )
                    //return PBICloudDatasetXmlaConnectivity.UnsupportedLiveConnectionToExternalDatasets;

                    if (dataset.Model.IsPushDataEnabled)
                        return PBICloudDatasetConnectionMode.UnsupportedPushDataset;

                    if (dataset.Model.IsExcelWorkbook)
                        return PBICloudDatasetConnectionMode.UnsupportedExcelWorkbookDataset;
                }

                return PBICloudDatasetConnectionMode.Supported;
            }

            static JsonElement GetDiagnostic(CloudWorkspace? workspace, CloudSharedModel dataset)
            {
                var diagnostic = new
                {
                    Workspace = new
                    {
                        workspace?.WorkspaceType,
                        workspace?.CapacitySkuType
                    },
                    Dataset = new
                    {
                        dataset.WorkspaceType,
                        dataset.Permissions,
                    },
                    Model = new
                    {
                        IsNull = dataset.Model is null,
                        dataset.Model?.Permissions,
                        dataset.Model?.IsHidden,
                        dataset.Model?.IsCloudModel,
                        dataset.Model?.IsOnPremModel,
                        dataset.Model?.IsExcelWorkbook,
                        dataset.Model?.IsWritablePbixModel,
                        dataset.Model?.IsWriteableModel,
                        dataset.Model?.IsPushDataEnabled,
                        dataset.Model?.IsPushStreaming,
                        dataset.Model?.DirectQueryMode,
                        dataset.Model?.InsightsSupported,
                    }
                };

                var diagnosticString = JsonSerializer.Serialize(diagnostic, AppEnvironment.DefaultJsonOptions);
                var diagnosticJson = JsonSerializer.Deserialize<JsonElement>(diagnosticString);

                return diagnosticJson;
            }
        }

        public Stream ExportVpax(PBICloudDataset dataset, bool includeTomModel, bool includeVpaModel, bool readStatisticsFromData, int sampleRows)
        {
            var (connectionString, databaseName) = dataset.GetConnectionParameters(CurrentAuthentication?.AccessToken);
            var stream = VpaxToolsHelper.ExportVpax(connectionString, databaseName, includeTomModel, includeVpaModel, readStatisticsFromData, sampleRows);

            return stream;
        }

        public string Update(PBICloudDataset dataset, IEnumerable<FormattedMeasure> measures)
        {
            var (connectionString, databaseName) = dataset.GetConnectionParameters(CurrentAuthentication?.AccessToken);
            var databaseETag = TabularModelHelper.Update(connectionString, databaseName, measures);

            return databaseETag;
        }

        public async Task<string?> GetAccountAvatarAsync()
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(CurrentAuthentication?.CreateAuthorizationHeader());

            var requestUri = new Uri(PBIApiUri, relativeUri: GetResourceUserPhotoRequestUri.FormatInvariant(CurrentAuthentication?.Account.Username));
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

        private void RefreshCurrentAccount()
        {
            var currentAuthentication = CurrentAuthentication ?? throw new BravoUnexpectedException("CurrentAuthentication is null");
            var currentAccountChanged = currentAuthentication.Account.HomeAccountId.Identifier.Equals(CurrentAccount?.Identifier) == false;
            if (currentAccountChanged)
            {
                var account = new BravoAccount
                {
                    Identifier = currentAuthentication.Account.HomeAccountId.Identifier,
                    UserPrincipalName = currentAuthentication.Account.Username,
                    Username = currentAuthentication.ClaimsPrincipal.FindFirst((c) => c.Type == "name")?.Value,
                };

                CurrentAccount = account;
            }
        }

        private async Task<IEnumerable<CloudWorkspace>> GetWorkspacesAsync()
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(CurrentAuthentication?.CreateAuthorizationHeader());

            var requestUri = new Uri(PBIApiUri, relativeUri: GetWorkspacesRequestUri);
            using var response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var workspaces = JsonSerializer.Deserialize<IEnumerable<CloudWorkspace>>(content, _jsonOptions);

            return workspaces?.ToArray() ?? Array.Empty<CloudWorkspace>();
        }

        private async Task<IEnumerable<CloudSharedModel>> GetSharedDatasetsAsync()
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(CurrentAuthentication?.CreateAuthorizationHeader());

            var requestUri = new Uri(_authenticationService.TenantCluster, relativeUri: GetGallerySharedDatasetsRequestUri);
            using var response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var datasets = JsonSerializer.Deserialize<IEnumerable<CloudSharedModel>>(content, _jsonOptions);

            return datasets?.ToArray() ?? Array.Empty<CloudSharedModel>();
        }
    }
}
