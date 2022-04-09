namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    using Sqlbi.Bravo.Infrastructure.Contracts.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System.Diagnostics;
    using System.Linq;

    [DebuggerDisplay("{Name}, {AuthorityUri}")]
    internal class PBICloudEnvironment: IPBICloudEnvironment
    {
        public PBICloudEnvironmentType? Type { get; set; }

        public string? AzureADAuthority { get; set; }

        public string? AzureADClientId { get; set; }

        public string? AzureADRedirectAddress { get; set; }

        public string? AzureADResource { get; set; }

        public string[]? AzureADScopes { get; set; }

        public string? ServiceEndpoint { get; set; }

        public static PBICloudEnvironment CreateFrom(PBICloudEnvironmentType pbicloudEnvironmentType, GlobalServiceEnvironment globalServiceEnvironment)
        {
            var authorityService = globalServiceEnvironment.Services?.Single((s) => "aad".EqualsI(s.Name));
            var powerbiService = globalServiceEnvironment.Services?.Single((s) => "powerbi-backend".EqualsI(s.Name));
            var powerbiClient = globalServiceEnvironment.Clients?.Single((c) => "powerbi-desktop".EqualsI(c.Name));

            BravoUnexpectedException.ThrowIfNull(authorityService);
            BravoUnexpectedException.ThrowIfNull(powerbiService);
            BravoUnexpectedException.ThrowIfNull(powerbiClient);

            var pbicloudEnvironment = new PBICloudEnvironment
            {
                Type = pbicloudEnvironmentType,
                AzureADAuthority = authorityService.Endpoint,
                AzureADClientId = powerbiClient.AppId,
                AzureADRedirectAddress = powerbiClient.RedirectUri,
                AzureADResource = powerbiService.ResourceId,
                AzureADScopes = new string[] { $"{ powerbiService.ResourceId }/.default" },
                ServiceEndpoint = powerbiService.Endpoint
            };
            
            return pbicloudEnvironment;
        }
    }

    public interface IPBICloudEnvironment
    {
        /// <summary>
        /// Type of the PowerBI environment.
        /// </summary>
        PBICloudEnvironmentType? Type { get; set; }

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
    }
}
