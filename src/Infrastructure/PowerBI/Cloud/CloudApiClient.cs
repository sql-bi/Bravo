namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Authentication;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Contracts;
    using Sqlbi.Bravo.Models;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Net.Http;
    using System.Net.Http.Headers;

    public interface ICloudApiClient
    {
        Task<string?> GetUserPhotoAsync(AuthenticatedSession session, CancellationToken cancellationToken);

        Task<IEnumerable<PBICloudDataset>> GetDatasetsAsync(AuthenticatedSession session, CancellationToken cancellationToken);
    }

    internal class CloudApiClient : ICloudApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = false, // required by SharedDatasetModel LastRefreshTime/lastRefreshTime properties
        };

        public const string PBIDatasetProtocolScheme = "pbiazure";
        public const string PBIPremiumXmlaEndpointProtocolScheme = "powerbi";
        //public const string PBIPremiumDedicatedProtocolScheme = "pbidedicated";
        public const string ASAzureProtocolScheme = "asazure";
        //public const string ASAzureLinkProtocolScheme = "link";

        public CloudApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(ServiceCollectionExtensions.PowerBIApiHttpClientName);
        }

        public async Task<string?> GetUserPhotoAsync(AuthenticatedSession session, CancellationToken cancellationToken)
        {
            var relativeUri = "powerbi/version/201606/resource/userPhoto/?userId={0}".FormatInvariant(session.AuthenticationResult.Email);
            var requestUri = session.Environment.GetBackendRequestUri(relativeUri);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AuthenticationResult.AccessToken);

            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

            // Any non-200 response (no photo, server error, ...) is treated the same way: no photo to show.
            if (httpResponse.StatusCode != HttpStatusCode.OK)
                return null;

            using var bitmapStream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var bitmap = TryCreateBitmap(bitmapStream);
            if (bitmap is null)
                return null; // Invalid image data

            var mimeType = TryGetMimeType(bitmap);
            if (mimeType is null)
                return null; // Unknown image format

            var base64String = GetBase64String(bitmap);

            return "data:{0};base64,{1}".FormatInvariant(mimeType, base64String);

            static Bitmap? TryCreateBitmap(Stream stream)
            {
                try
                {
                    return new Bitmap(stream);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            static string? TryGetMimeType(Bitmap bitmap)
                => ImageCodecInfo.GetImageDecoders().FirstOrDefault((c) => c.FormatID == bitmap.RawFormat.Guid)?.MimeType;

            static string GetBase64String(Bitmap bitmap)
            {
                using var stream = new MemoryStream();
                bitmap.Save(stream, bitmap.RawFormat);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public async Task<IEnumerable<PBICloudDataset>> GetDatasetsAsync(AuthenticatedSession session, CancellationToken cancellationToken)
        {
            var cloudWorkspaces = await GetCloudWorkspacesAsync(session, cancellationToken);
            var cloudSharedModels = await GetCloudSharedModelsAsync(session, cancellationToken);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
            {
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(CloudApiClient) }.{ nameof(GetDatasetsAsync) }.{ nameof(cloudWorkspaces) }", content: JsonSerializer.Serialize(cloudWorkspaces));
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(CloudApiClient) }.{ nameof(GetDatasetsAsync) }.{ nameof(cloudSharedModels) }", content: JsonSerializer.Serialize(cloudSharedModels));
            }

            var datasets = cloudWorkspaces.Join(cloudSharedModels, (w) => w.ObjectId?.ToLowerInvariant(), (d) => d.ObjectId?.ToLowerInvariant(), (w, d) => PBICloudDataset.CreateFrom(session.Environment, w, d)).ToArray();

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(CloudApiClient) }.{ nameof(GetDatasetsAsync) }", content: JsonSerializer.Serialize(datasets));

            return datasets;
        }

        private async Task<IEnumerable<CloudWorkspace>> GetCloudWorkspacesAsync(AuthenticatedSession session, CancellationToken cancellationToken)
        {
            var baseUri = new Uri(session.Environment.ClusterUri);
            var relativeUri = "powerbi/databases/v201606/workspaces";
            var requestUri = new Uri(baseUri, relativeUri);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AuthenticationResult.AccessToken);

            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            var json = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(CloudApiClient) }.{ nameof(GetCloudWorkspacesAsync) }", json);

            return JsonSerializer.Deserialize<CloudWorkspace[]>(json, _jsonOptions) ?? [];
        }

        private async Task<IEnumerable<CloudSharedModel>> GetCloudSharedModelsAsync(AuthenticatedSession session, CancellationToken cancellationToken)
        {
            var baseUri = new Uri(session.Environment.ClusterUri);
            var relativeUri = "metadata/v201901/gallery/sharedDatasets";
            var requestUri = new Uri(baseUri, relativeUri);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AuthenticationResult.AccessToken);

            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            var json = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(CloudApiClient) }.{ nameof(GetCloudSharedModelsAsync) }", json);

            return JsonSerializer.Deserialize<CloudSharedModel[]>(json, _jsonOptions) ?? [];
        }
    }
}
