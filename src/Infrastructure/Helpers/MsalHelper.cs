namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Microsoft.Identity.Client;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Models.PBICloud;

    internal static class MsalHelper
    {
        private const string SystemBrowserRedirectUri = "http://localhost";

        public static IPublicClientApplication CreatePublicClientApplication(IPBICloudEnvironment pbicloudEnvironment)
        {
            var useEmbeddedBrowser = !UserPreferences.Current.UseSystemBrowserForAuthentication;
            var redirectUri = (useEmbeddedBrowser ? pbicloudEnvironment.AzureADRedirectAddress : SystemBrowserRedirectUri);

            var publicClient = PublicClientApplicationBuilder.Create(pbicloudEnvironment.AzureADClientId)
                .WithAuthority(pbicloudEnvironment.AzureADAuthority)
                .WithRedirectUri(redirectUri)
                // TODO: should we add logging ? .WithLogging()
                .Build();

            TokenCacheHelper.EnableSerialization(publicClient.UserTokenCache);

            return publicClient;
        }
    }
}
