namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Microsoft.Identity.Client;
    using Microsoft.Identity.Client.Desktop;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Models.PBICloud;

    internal static class MsalHelper
    {
        private const string SystemBrowserRedirectUri = "http://localhost";
        private const string MicrosoftAccountOnlyQueryParameter = "msafed=0"; // Restrict logins to only AAD based organizational accounts

        public static IPublicClientApplication CreatePublicClientApplication(CloudEnvironment environment)
        {
            var useEmbeddedBrowser = !UserPreferences.Current.UseSystemBrowserForAuthentication;
            var redirectUri = (useEmbeddedBrowser ? environment.RedirectUri : SystemBrowserRedirectUri);

            // TODO: should we add logging .WithLogging() ??
            var builder = PublicClientApplicationBuilder.Create(environment.ClientId).WithAuthority(environment.AuthorityUri).WithRedirectUri(redirectUri);
            {
                if (useEmbeddedBrowser)
                    builder.WithWindowsEmbeddedBrowserSupport();
            }
            var publicClient = builder.Build();

            TokenCacheHelper.RegisterCache(publicClient.UserTokenCache);

            return publicClient;
        }

        public static async Task<PBICloudAuthenticationResult> AcquireTokenSilentAsync(string email, CloudEnvironment environment, CancellationToken cancellationToken)
        {
            var extraQueryParameters = MicrosoftAccountOnlyQueryParameter;
            var scopes = new string[] { $"{environment.ResourceId}/.default" };
            var loginHint = email;

            var publicClient = CreatePublicClientApplication(environment);
            var msalAuthenticationResult = await publicClient.AcquireTokenSilent(scopes, loginHint).WithExtraQueryParameters(extraQueryParameters).ExecuteAsync(cancellationToken).ConfigureAwait(false);
            var pbicloudAuthenticationResult = new PBICloudAuthenticationResult(msalAuthenticationResult);

            return pbicloudAuthenticationResult;
        }

        public static async Task<PBICloudAuthenticationResult> AcquireTokenInteractiveAsync(string email, CloudEnvironment environment, string claims, CancellationToken cancellationToken)
        {
            var useEmbeddedBrowser = !UserPreferences.Current.UseSystemBrowserForAuthentication;
            var extraQueryParameters = MicrosoftAccountOnlyQueryParameter;
            var prompt = Prompt.SelectAccount; // Force a sign-in as the MSAL web browser might contain cookies for the current user and we don't necessarily want to re-sign-in the same user
            var scopes = new string[] { $"{environment.ResourceId}/.default" };
            var loginHint = email;

            var acquireTokenTask = ProcessHelper.RunOnUISynchronizationContextContext(async () =>
            {
                var publicClient = CreatePublicClientApplication(environment);
                var parameterBuilder = publicClient.AcquireTokenInteractive(scopes).WithExtraQueryParameters(extraQueryParameters).WithUseEmbeddedWebView(useEmbeddedBrowser).WithLoginHint(loginHint).WithPrompt(prompt).WithClaims(claims);

                if (useEmbeddedBrowser)
                {
                    var mainwindowHwnd = ProcessHelper.GetCurrentProcessMainWindowHandle();
                    parameterBuilder.WithParentActivityOrWindow(mainwindowHwnd);
                }

                var msalAuthenticationResult = await parameterBuilder.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                return msalAuthenticationResult;
            });

            var msalAuthenticationResult = await acquireTokenTask.ConfigureAwait(false);
            var pbicloudAuthenticationResult = new PBICloudAuthenticationResult(msalAuthenticationResult);

            return pbicloudAuthenticationResult;
        }

        //public static async Task<PBICloudAuthenticationResult> AcquireTokenByIntegratedWindowsAuthAsync(IPBICloudEnvironment environment, CancellationToken cancellationToken)
        //{
        //    var publicClient = CreatePublicClientApplication(environment);
        //    var msalAuthenticationResult = await publicClient.AcquireTokenByIntegratedWindowsAuth(environment.AzureADScopes).ExecuteAsync(cancellationToken).ConfigureAwait(false);
        //    var pbicloudAuthenticationResult = new PBICloudAuthenticationResult(msalAuthenticationResult);

        //    // TODO: Assert UPN from local windows account is equals to UPN from authentication result
        //    // BravoUnexpectedException.Assert(authenticationResult.ClaimsPrincipal.Identity.Name == account.Username);

        //    return pbicloudAuthenticationResult;
        //}

        public static async Task ClearTokenCacheAsync(CloudEnvironment environment)
        {
            var publicClient = CreatePublicClientApplication(environment);
            var cachedAccounts = (await publicClient.GetAccountsAsync().ConfigureAwait(false)).ToArray();

            foreach (var account in cachedAccounts)
            {
                await publicClient.RemoveAsync(account).ConfigureAwait(false);
            }
        }
    }
}
