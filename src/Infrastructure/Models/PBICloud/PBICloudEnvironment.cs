namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    using System.Diagnostics;

    [DebuggerDisplay("{Name}, {AuthorityUri}")]
    internal class PBICloudEnvironment: IPBICloudEnvironment
    {
        public PBICloudEnvironmentType? Type { get; set; }

        public string? AzureADAuthority { get; set; }

        public string? AzureADClientId { get; set; }

        public string? AzureADRedirectAddress { get; set; }

        public string? AzureADResource { get; set; }

        public string? GlobalServiceEndpoint { get; set; }
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
        /// Endpoint to communicate with the PowerBI global service
        /// </summary>
        string? GlobalServiceEndpoint { get; set; }
    }
}
