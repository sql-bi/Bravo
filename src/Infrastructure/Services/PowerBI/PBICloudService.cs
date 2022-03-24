namespace Sqlbi.Bravo.Infrastructure.Services.PowerBI
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Contracts.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.AnalyzeModel;
    using Sqlbi.Bravo.Services;
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
    using System.Threading;
    using System.Threading.Tasks;

    public interface IPBICloudService
    {
        Task<string?> GetAccountAvatarAsync();

        Task<IEnumerable<PBICloudDataset>> GetDatasetsAsync(CancellationToken cancellationToken);
     }

    internal class PBICloudService : IPBICloudService
    {
        private const string GetWorkspacesRequestUri = "powerbi/databases/v201606/workspaces";
        private const string GetGallerySharedDatasetsRequestUri = "metadata/v201901/gallery/sharedDatasets";
        private const string GetResourceUserPhotoRequestUri = "powerbi/version/201606/resource/userPhoto/?userId={0}";

        private readonly HttpClient _httpClient;
        private readonly IAuthenticationService _authenticationService;
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

        public PBICloudService(IAuthenticationService authenticationService, HttpClient httpClient)
        {
            _authenticationService = authenticationService;
            _httpClient = httpClient;
        }

        public async Task<string?> GetAccountAvatarAsync()
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(_authenticationService.PBICloudAuthentication.CreateAuthorizationHeader());

            var requestUri = new Uri(PBIApiUri, relativeUri: GetResourceUserPhotoRequestUri.FormatInvariant(_authenticationService.PBICloudAuthentication.Account.Username));
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

        public async Task<IEnumerable<PBICloudDataset>> GetDatasetsAsync(CancellationToken cancellationToken)
        {
            var onlineWorkspaces = await GetWorkspacesAsync(cancellationToken);
            var onlineDatasets = await GetSharedDatasetsAsync(cancellationToken);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
            {
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudService) }.{ nameof(GetDatasetsAsync) }.{ nameof(onlineWorkspaces) }", content: JsonSerializer.Serialize(onlineWorkspaces));
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudService) }.{ nameof(GetDatasetsAsync) }.{ nameof(onlineDatasets) }", content: JsonSerializer.Serialize(onlineDatasets));
            }

            var datasets = onlineWorkspaces.Join(onlineDatasets, (w) => w.ObjectId?.ToLowerInvariant(), (d) => d.ObjectId?.ToLowerInvariant(), PBICloudDataset.CreateFrom).ToArray();

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudService) }.{ nameof(GetDatasetsAsync) }", content: JsonSerializer.Serialize(datasets));

            return datasets;
        }

        private async Task<IEnumerable<CloudWorkspace>> GetWorkspacesAsync(CancellationToken cancellationToken)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(_authenticationService.PBICloudAuthentication.CreateAuthorizationHeader());

            var requestUri = new Uri(PBIApiUri, relativeUri: GetWorkspacesRequestUri);
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            
            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudService) }.{ nameof(GetWorkspacesAsync) }", content);

            var workspaces = JsonSerializer.Deserialize<IEnumerable<CloudWorkspace>>(content, _jsonOptions);

            return workspaces?.ToArray() ?? Array.Empty<CloudWorkspace>();
        }

        private async Task<IEnumerable<CloudSharedModel>> GetSharedDatasetsAsync(CancellationToken cancellationToken)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(_authenticationService.PBICloudAuthentication.CreateAuthorizationHeader());

            var requestUri = new Uri(_authenticationService.PBICloudTenantCluster, relativeUri: GetGallerySharedDatasetsRequestUri);
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            
            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudService) }.{ nameof(GetSharedDatasetsAsync) }", content);

            var datasets = JsonSerializer.Deserialize<IEnumerable<CloudSharedModel>>(content, _jsonOptions);

            return datasets?.ToArray() ?? Array.Empty<CloudSharedModel>();
        }
    }
}