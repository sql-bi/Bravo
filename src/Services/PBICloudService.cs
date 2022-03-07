namespace Sqlbi.Bravo.Services
{
    using Microsoft.Identity.Client;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Contracts.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.AnalyzeModel;
    using Sqlbi.Bravo.Models.FormatDax;
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

        Task<string?> GetAccountAvatarAsync();

        Stream GetVpax(PBICloudDataset dataset);

        TabularDatabase GetDatabase(PBICloudDataset dataset);

        string Update(PBICloudDataset dataset, IEnumerable<FormattedMeasure> measures);
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
        public static readonly Uri PBIDatasetServerUri = new($"{ PBIDatasetProtocolScheme }://api.powerbi.com");
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
                throw new BravoException(BravoProblem.SignInMsalTimeoutExpired);
            }

           RefreshCurrentAccount();
        }

        public async Task SignOutAsync()
        {
            CurrentAccount = null;

            await _authenticationService.ClearTokenCacheAsync().ConfigureAwait(false);

            BravoUnexpectedException.Assert(CurrentAuthentication is null);
        }

        public async Task<IEnumerable<PBICloudDataset>> GetDatasetsAsync()
        {
            var onlineWorkspaces = await GetWorkspacesAsync();
            var onlineDatasets = await GetSharedDatasetsAsync();

            if (AppEnvironment.IsDiagnosticLevelVerbose)
            {
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudService) }-{ nameof(GetDatasetsAsync) }-{ nameof(onlineWorkspaces) }", content: JsonSerializer.Serialize(onlineWorkspaces));
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudService) }-{ nameof(GetDatasetsAsync) }-{ nameof(onlineDatasets) }", content: JsonSerializer.Serialize(onlineDatasets));
            }

            var datasets = onlineWorkspaces.Join(onlineDatasets, (w) => w.ObjectId?.ToLowerInvariant(), (d) => d.ObjectId?.ToLowerInvariant(), PBICloudDataset.CreateFrom).ToArray();

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudService) }-{ nameof(GetDatasetsAsync) }", content: JsonSerializer.Serialize(datasets));

            return datasets;
        }

        public async Task<string?> GetAccountAvatarAsync()
        {
            BravoUnexpectedException.ThrowIfNull(CurrentAuthentication?.AccessToken);

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(CurrentAuthentication.CreateAuthorizationHeader());

            var requestUri = new Uri(PBIApiUri, relativeUri: GetResourceUserPhotoRequestUri.FormatInvariant(CurrentAuthentication.Account.Username));
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

        public Stream GetVpax(PBICloudDataset dataset)
        {
            BravoUnexpectedException.ThrowIfNull(CurrentAuthentication?.AccessToken);

            using var connection = TabularConnectionWrapper.ConnectTo(dataset, CurrentAuthentication.AccessToken);
            var stream = VpaxToolsHelper.GetVpax(connection);

            return stream;
        }

        public TabularDatabase GetDatabase(PBICloudDataset dataset)
        {
            BravoUnexpectedException.ThrowIfNull(CurrentAuthentication?.AccessToken);
            BravoUnexpectedException.ThrowIfNull(dataset.DisplayName);
            TabularDatabase database;

            if (dataset.IsXmlaEndPointSupported)
            {
                using var connection = TabularConnectionWrapper.ConnectTo(dataset, CurrentAuthentication.AccessToken);
                database = TabularDatabase.CreateFrom(connection);
            }
            else
            {
                using var connection = AdomdConnectionWrapper.ConnectTo(dataset, CurrentAuthentication.AccessToken);
                database = TabularDatabase.CreateFromDmvSchema(connection);

                database.Features &= ~TabularDatabaseFeature.AnalyzeModelAll;
                database.Features &= ~TabularDatabaseFeature.FormatDaxAll;
                database.FeatureUnsupportedReasons |= TabularDatabaseFeatureUnsupportedReason.XmlaEndpointNotSupported;
            }

            database.Features &= ~TabularDatabaseFeature.ManageDatesAll;
            database.FeatureUnsupportedReasons |= TabularDatabaseFeatureUnsupportedReason.ManageDatesPBIDesktopModelOnly;

            return database;
        }

        public string Update(PBICloudDataset dataset, IEnumerable<FormattedMeasure> measures)
        {
            BravoUnexpectedException.ThrowIfNull(CurrentAuthentication?.AccessToken);

            using var connection = TabularConnectionWrapper.ConnectTo(dataset, CurrentAuthentication.AccessToken);
            var databaseETag = TabularModelHelper.Update(connection.Database, measures);

            return databaseETag;
        }

        private void RefreshCurrentAccount()
        {
            BravoUnexpectedException.ThrowIfNull(CurrentAuthentication);

            var currentAccountChanged = CurrentAuthentication.Account.HomeAccountId.Identifier.Equals(CurrentAccount?.Identifier) == false;
            if (currentAccountChanged)
            {
                var account = new BravoAccount
                {
                    Identifier = CurrentAuthentication.Account.HomeAccountId.Identifier,
                    UserPrincipalName = CurrentAuthentication.Account.Username,
                    Username = CurrentAuthentication.ClaimsPrincipal.FindFirst((c) => c.Type == "name")?.Value,
                };

                CurrentAccount = account;
            }
        }

        private async Task<IEnumerable<CloudWorkspace>> GetWorkspacesAsync()
        {
            BravoUnexpectedException.ThrowIfNull(CurrentAuthentication?.AccessToken);

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(CurrentAuthentication.CreateAuthorizationHeader());

            var requestUri = new Uri(PBIApiUri, relativeUri: GetWorkspacesRequestUri);
            using var response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudService) }-{ nameof(GetWorkspacesAsync) }", content);

            var workspaces = JsonSerializer.Deserialize<IEnumerable<CloudWorkspace>>(content, _jsonOptions);

            return workspaces?.ToArray() ?? Array.Empty<CloudWorkspace>();
        }

        private async Task<IEnumerable<CloudSharedModel>> GetSharedDatasetsAsync()
        {
            BravoUnexpectedException.ThrowIfNull(CurrentAuthentication?.AccessToken);

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(CurrentAuthentication.CreateAuthorizationHeader());

            var requestUri = new Uri(_authenticationService.TenantCluster, relativeUri: GetGallerySharedDatasetsRequestUri);
            using var response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudService) }-{ nameof(GetSharedDatasetsAsync) }", content);

            var datasets = JsonSerializer.Deserialize<IEnumerable<CloudSharedModel>>(content, _jsonOptions);

            return datasets?.ToArray() ?? Array.Empty<CloudSharedModel>();
        }
    }
}