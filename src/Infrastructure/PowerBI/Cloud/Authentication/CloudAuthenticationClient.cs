namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Authentication
{
    using Microsoft.Identity.Client;
    using Microsoft.Identity.Client.Desktop;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud;
    using Msal = Microsoft.Identity.Client;

    public interface ICloudAuthenticationClient
    {
        Task<AuthenticationResult> AcquireTokenAsync(CloudEnvironment environment, string email, CancellationToken cancellationToken);

        Task ClearTokenCacheAsync(CloudEnvironment environment);
    }

    /// <summary>
    /// Handles authentication with Microsoft Entra ID (Azure AD) using MSAL.NET, including token acquisition and cache management.
    /// </summary>
    internal sealed class CloudAuthenticationClient : ICloudAuthenticationClient
    {
        private const string SystemBrowserRedirectUri = "http://localhost";
        private const string OrganizationalAccountsOnlyQueryParameter = "msafed=0"; // no Microsoft accounts (MSA) allowed

        public async Task<AuthenticationResult> AcquireTokenAsync(
            CloudEnvironment environment, string email, CancellationToken cancellationToken)
        {
            var client = CreatePublicClient(environment);
            var scopes = CreateScopes(environment);
            try
            {
                // TODO: no B2B/guest-tenant support yet - acquiring a token for a workspace hosted in a tenant other than the
                // user's home tenant would require passing a tenantId here and calling WithTenantId(tenantId) on the builders.
                var msalResult = await AcquireTokenSilentAsync(client, scopes, email, cancellationToken).ConfigureAwait(false);
                return AuthenticationResult.From(msalResult);
            }
            // Catching MsalServiceException (not just its MsalUiRequiredException subclass) also covers Conditional
            // Access claims challenges, which MSAL surfaces as a plain MsalServiceException with a non-empty Claims
            // See https://learn.microsoft.com/entra/msal/dotnet/advanced/exceptions/#handling-claim-challenge-exceptions-in-msalnet
            catch (MsalServiceException ex)
            {
                var msalResult = await AcquireTokenInteractiveAsync(client, scopes, email, ex.Claims, cancellationToken).ConfigureAwait(false);
                return AuthenticationResult.From(msalResult);
            }
        }

        public async Task ClearTokenCacheAsync(CloudEnvironment environment)
        {
            var client = CreatePublicClient(environment);
            var accounts = (await client.GetAccountsAsync().ConfigureAwait(false)).ToArray();

            foreach (var account in accounts)
            {
                await client.RemoveAsync(account).ConfigureAwait(false);
            }
        }

        private static async Task<Msal.AuthenticationResult> AcquireTokenSilentAsync(
            IPublicClientApplication client, string[] scopes, string email, CancellationToken cancellationToken)
        {
            var extraQueryParameters = OrganizationalAccountsOnlyQueryParameter;
            var loginHint = email;

            var builder = client.AcquireTokenSilent(scopes, loginHint)
                .WithExtraQueryParameters(extraQueryParameters);

            return await builder.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<Msal.AuthenticationResult> AcquireTokenInteractiveAsync(
            IPublicClientApplication client, string[] scopes, string email, string claims, CancellationToken cancellationToken)
        {
            var useEmbeddedBrowser = !UserPreferences.Current.UseSystemBrowserForAuthentication;
            var extraQueryParameters = OrganizationalAccountsOnlyQueryParameter;
            var prompt = Prompt.SelectAccount;
            var loginHint = email;

            // AcquireTokenInteractive(scopes) captures SynchronizationContext.Current so the builder must be
            // created on the UI thread to ensure that the interactive flow is executed on the UI thread.
            var builder = ProcessHelper.RunWithUISynchronizationContext(() =>
            {
                return client.AcquireTokenInteractive(scopes);
            });

            builder
                .WithExtraQueryParameters(extraQueryParameters)
                .WithUseEmbeddedWebView(useEmbeddedBrowser)
                .WithLoginHint(loginHint)
                .WithPrompt(prompt)
                .WithClaims(claims);

            if (useEmbeddedBrowser)
            {
                var windowHandle = ProcessHelper.GetCurrentProcessMainWindowHandle();
                builder.WithParentActivityOrWindow(windowHandle);
            }

            return await builder.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }

        private static IPublicClientApplication CreatePublicClient(CloudEnvironment environment)
        {
            var useEmbeddedBrowser = !UserPreferences.Current.UseSystemBrowserForAuthentication;
            var redirectUri = (useEmbeddedBrowser ? environment.RedirectUri : SystemBrowserRedirectUri);

            var builder = PublicClientApplicationBuilder.Create(environment.ClientId)
                .WithAuthority(environment.AuthorityUri)
                .WithRedirectUri(redirectUri);

            if (useEmbeddedBrowser)
                builder.WithWindowsEmbeddedBrowserSupport();

            var client = builder.Build();

            TokenCacheHelper.RegisterCache(client.UserTokenCache);

            return client;
        }

        private static string[] CreateScopes(CloudEnvironment environment)
        {
            var resource = environment.ResourceId.TrimEnd('/');
            return [$"{resource}/.default"];
        }
    }
}
