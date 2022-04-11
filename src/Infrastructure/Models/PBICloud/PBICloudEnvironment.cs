namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    using Sqlbi.Bravo.Infrastructure.Contracts.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json.Serialization;

    [DebuggerDisplay("{Type}, {AzureADAuthority}")]
    internal class PBICloudEnvironment: IPBICloudEnvironment
    {
        [JsonPropertyName("type")]
        public PBICloudEnvironmentType? Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

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

        [JsonPropertyName("isValid")]
        public bool IsValid => Type is not null;

        public static PBICloudEnvironment CreateFrom(GlobalServiceEnvironment globalServiceEnvironment)
        {
            var authorityService = globalServiceEnvironment.Services?.SingleOrDefault((s) => "aad".EqualsI(s.Name));
            var powerbiService = globalServiceEnvironment.Services?.SingleOrDefault((s) => "powerbi-backend".EqualsI(s.Name));
            var powerbiClient = globalServiceEnvironment.Clients?.SingleOrDefault((c) => "powerbi-desktop".EqualsI(c.Name));

            var pbicloudEnvironment = new PBICloudEnvironment
            {
                Type = globalServiceEnvironment.CloudName?.ToCloudEnvironmentType(),
                Name = globalServiceEnvironment.CloudName,
                AzureADAuthority = authorityService?.Endpoint,
                AzureADClientId = powerbiClient?.AppId,
                AzureADRedirectAddress = powerbiClient?.RedirectUri,
                AzureADResource = powerbiService?.ResourceId,
                AzureADScopes = null,
                ServiceEndpoint = powerbiService?.Endpoint,
                ClusterEndpoint = null
            };

            if (pbicloudEnvironment.Type is not null)
                pbicloudEnvironment.Name += $" - { pbicloudEnvironment.Type.Value.ToCloudEnvironmentDescription() }";

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
        PBICloudEnvironmentType? Type { get; set; }
        
        /// <summary>
        /// Cloud environment name.
        /// </summary>
        string? Name { get; set; }

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

        bool IsValid { get; }
    }
}
