namespace Sqlbi.Bravo.Infrastructure.Configuration.Settings
{
    using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
    using System.Text.Json.Serialization;

    public class ExperimentalSettings
    {
        //[JsonPropertyName("useIntegratedWindowsAuthenticationSso")]
        //public bool? UseIntegratedWindowsAuthenticationSso { get; }

        [JsonPropertyName("useSystemBrowserForAuthentication")]
        public bool? UseSystemBrowserForAuthentication { get; set; }

        [JsonPropertyName("pbiEnvironment")]
        public PBICloudEnvironmentType? PBIEnvironment { get; set; }
    }
}
