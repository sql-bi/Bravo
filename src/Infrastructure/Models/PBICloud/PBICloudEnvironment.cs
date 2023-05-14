namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    using Sqlbi.Bravo.Infrastructure.Contracts.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json.Serialization;

    [DebuggerDisplay("{Type}, {AzureADAuthority}")]
    public class PBICloudEnvironment: IPBICloudEnvironment
    {
        [JsonPropertyName("type")]
        public PBICloudEnvironmentType Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("aadAuthority")]
        public string? AzureADAuthority { get; set; }

        [JsonPropertyName("aadClientId")]
        public string? AzureADClientId { get; set; }

        [JsonPropertyName("aadRedirectAddress")]
        public string? AzureADRedirectAddress { get; set; }

        [JsonPropertyName("aadResource")]
        public string? AzureADResource { get; set; }

        [JsonPropertyName("aadScopes")]
        public string[]? AzureADScopes { get; set; }

        [JsonPropertyName("serviceEndpoint")]
        public string? ServiceEndpoint { get; set; }

        [JsonPropertyName("clusterEndpoint")]
        public string? ClusterEndpoint { get; set; }

        [JsonPropertyName("identityProvider")]
        public string? IdentityProvider => $"{AzureADAuthority}, {AzureADResource}, {AzureADClientId}";

        [JsonIgnore]
        public bool IsMicrosoftInternal => Type == PBICloudEnvironmentType.Custom && Name.EqualsI(PBICloudEnvironmentTypeExtensions.PpeCloudName);

        public Uri GetServiceEndpointUri(string path)
        {
            var baseUri = new Uri(ServiceEndpoint!, UriKind.Absolute);
            return new Uri(baseUri, relativeUri: path);
        }

        internal static PBICloudEnvironment CreateFrom(GlobalServiceEnvironment globalServiceEnvironment)
        {
            var azureActiveDirectoryService = globalServiceEnvironment.Services?.SingleOrDefault((s) => "aad".EqualsI(s.Name)); // AAD common
            var powerbiBackendService = globalServiceEnvironment.Services?.SingleOrDefault((s) => "powerbi-backend".EqualsI(s.Name));
            var powerbiDesktopClient = globalServiceEnvironment.Clients?.SingleOrDefault((c) => "powerbi-desktop".EqualsI(c.Name));

            var pbicloudEnvironment = new PBICloudEnvironment
            {
                Type = globalServiceEnvironment.CloudName.ToCloudEnvironmentType(),
                Name = globalServiceEnvironment.CloudName,
                Description = globalServiceEnvironment.CloudName.ToCloudEnvironmentDescription(),
                AzureADAuthority = azureActiveDirectoryService?.Endpoint,
                AzureADClientId = powerbiDesktopClient?.AppId,
                AzureADRedirectAddress = powerbiDesktopClient?.RedirectUri,
                AzureADResource = powerbiBackendService?.ResourceId,
                AzureADScopes = null,
                ServiceEndpoint = powerbiBackendService?.Endpoint,
                ClusterEndpoint = null
            };
          
            if (pbicloudEnvironment.AzureADResource is not null)
                pbicloudEnvironment.AzureADScopes = new string[] { $"{ pbicloudEnvironment.AzureADResource }/.default" };

            return pbicloudEnvironment;
        }

        public override bool Equals(object? obj)
        {
            return obj is PBICloudEnvironment environment &&
                   Type == environment.Type &&
                   Name == environment.Name &&
                   AzureADAuthority == environment.AzureADAuthority &&
                   AzureADClientId == environment.AzureADClientId &&
                   AzureADRedirectAddress == environment.AzureADRedirectAddress &&
                   AzureADResource == environment.AzureADResource &&
                   ServiceEndpoint == environment.ServiceEndpoint;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, AzureADAuthority, AzureADClientId, AzureADRedirectAddress, AzureADResource, ServiceEndpoint);
        }
    }

    public interface IPBICloudEnvironment
    {
        /// <summary>
        /// Type of the PowerBI environment.
        /// </summary>
        PBICloudEnvironmentType Type { get; set; }
        
        /// <summary>
        /// Cloud environment name.
        /// </summary>
        string? Name { get; set; }

        /// <summary>
        /// Cloud environment description.
        /// </summary>
        string? Description { get; set; }

        /// <summary>
        /// Azure Active Directory Secure Token Service (STS) Authority
        /// </summary>
        string? AzureADAuthority { get; set; }

        /// <summary>
        /// Azure Active Directory (AAD) ClientId for the AAD application
        /// </summary>
        string? AzureADClientId { get; set; }

        /// <summary>
        /// Azure Active Directory (AAD) Redirect Address for AAD application
        /// </summary>
        string? AzureADRedirectAddress { get; set; }

        /// <summary>
        /// Azure Active Directory Resource to authenticate against
        /// </summary>
        string? AzureADResource { get; set; }

        /// <summary>
        /// Azure Active Directory scopes requested to access the protected <see cref="AzureADResource"/>
        /// </summary>
        string[]? AzureADScopes { get; set; }

        /// <summary>
        /// Endpoint to communicate with the PowerBI service
        /// </summary>
        string? ServiceEndpoint { get; set; }

        /// <summary>
        /// Fixed tenant cluster endpoint
        /// </summary>
        string? ClusterEndpoint { get; set; }

        /// <summary>
        /// MSOLAP OLEDB provider 'Identity Provider'
        /// </summary>
        string? IdentityProvider { get; }

        [JsonIgnore]
        bool IsMicrosoftInternal { get; }

        Uri GetServiceEndpointUri(string path);
    }
}
