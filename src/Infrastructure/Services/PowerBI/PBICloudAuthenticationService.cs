namespace Sqlbi.Bravo.Infrastructure.Services.PowerBI
{
    using Microsoft.Identity.Client;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IPBICloudAuthenticationService
    {
        AuthenticationResult? Authentication { get; }

        Uri TenantCluster { get; }

        Task AcquireTokenAsync(bool silent = false, string? loginHint = null, TimeSpan? timeout = null);

        Task<bool> RefreshTokenAsync();

        Task ClearTokenCacheAsync();
    }

    internal class PBICloudAuthenticationService : IPBICloudAuthenticationService
    {
        private const string MicrosoftAccountOnlyQueryParameter = "msafed=0"; // Restrict logins to only AAD based organizational accounts

        private readonly static SemaphoreSlim _authenticationSemaphore = new(1, 1); // TODO: dispose
        private readonly static SemaphoreSlim _publicClientSemaphore = new(1, 1); // TODO: dispose
        private readonly IPBICloudSettingsService _pbicloudSettings;

        private IPublicClientApplication? _publicClient;

        public PBICloudAuthenticationService(IPBICloudSettingsService pbicloudSetting)
        {
            _pbicloudSettings = pbicloudSetting;
        }

        private IPublicClientApplication PublicClient
        {
            get
            {
                BravoUnexpectedException.ThrowIfNull(_publicClient);
                return _publicClient;
            }
        }

        public AuthenticationResult? Authentication { get; private set; }

        public Uri TenantCluster => new(_pbicloudSettings.TenantCluster.FixedClusterUri);

        public async Task ClearTokenCacheAsync()
        {
            await _authenticationSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                Authentication = null;

                _ = await ProcessHelper.RunOnUISynchronizationContextContext(async () =>
                {
                    await EnsureInitializedAsync().ConfigureAwait(false);

                    var accounts = (await PublicClient.GetAccountsAsync().ConfigureAwait(false)).ToArray();

                    foreach (var account in accounts)
                    {
                        await PublicClient.RemoveAsync(account).ConfigureAwait(false);
                    }

                    return true;
                });
            }
            finally
            {
                _authenticationSemaphore.Release();
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                await AcquireTokenAsync(silentOnly: true).ConfigureAwait(false);

                return true;
            }
            catch (MsalUiRequiredException)
            {
                return false;
            }
        }

        public async Task AcquireTokenAsync(bool silentOnly = false, string? loginHint = null, TimeSpan? timeout = null)
        {
            await _authenticationSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                _ = await ProcessHelper.RunOnUISynchronizationContextContext(async () =>
                { 
                    await EnsureInitializedAsync().ConfigureAwait(false);

                    using var cancellationTokenSource = new CancellationTokenSource();
                    if (timeout.HasValue) cancellationTokenSource.CancelAfter(timeout.Value);

                    var previousIdentifier = Authentication?.Account.HomeAccountId.Identifier;
                    var previousAuthentication = Authentication;

                    Authentication = await AcquireTokenImplAsync(silentOnly, previousIdentifier, loginHint, cancellationTokenSource.Token).ConfigureAwait(false);

                    var accountChanged = !Authentication.Account.HomeAccountId.Equals(previousAuthentication?.Account.HomeAccountId);
                    if (accountChanged)
                    { 
                        await _pbicloudSettings.RefreshAsync(Authentication.AccessToken).ConfigureAwait(false);
                    }

                    return true;
                });
            }
            finally
            {
                _authenticationSemaphore.Release();
            }
        }

        private async Task<AuthenticationResult> AcquireTokenImplAsync(bool silentOnly, string? identifier, string? loginHint, CancellationToken cancellationToken)
        {
            // Use account used to signed-in in Windows (WAM). WAM will always get an account in the cache so, if we want to have a chance to select the accounts interactively, we need to force the non-account.
            //identifier = PublicClientApplication.OperatingSystemAccount;

            // Use one of the Accounts known by Windows (WAM), if a null account identifier is provided then force WAM to display the dialog with the accounts
            var account = await PublicClient.GetAccountAsync(identifier).ConfigureAwait(false);
            var scopes = _pbicloudSettings.CloudEnvironment.AzureADScopes;

            try
            {
                // Try to acquire an access token from the cache, if UI interaction is required, MsalUiRequiredException will be thrown.
                var authenticationResult = await PublicClient.AcquireTokenSilent(scopes, account).WithExtraQueryParameters(MicrosoftAccountOnlyQueryParameter).ExecuteAsync(cancellationToken).ConfigureAwait(false);
                return authenticationResult;

                //if ()
                //{
                //    AcquireTokenSilent(scopes, account)
                //}
                //else
                //{
                //    if (AppEnvironment.IsIntegratedWindowsAuthenticationSsoSupportEnabled)
                //    {
                //        ////var authenticationResult = await AcquireTokenByIntegratedWindowsAuth(scopes).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                //        ////// Assert UPN from local windows account is equals to UPN from authentication result
                //        ////BravoUnexpectedException.Assert(authenticationResult.ClaimsPrincipal.Identity.Name == account.Username);
                //    }
                //    else
                //    {
                //        ////AcquireTokenInteractive(scopes)
                //    }
                //}
            }
            catch (MsalUiRequiredException ex)
            {
                // Re-throw exception if silent-only token acquisition was requested
                if (silentOnly) throw;
                try
                {
                    // *** EmbeddedWebView requirements ***
                    // Requires VS project OutputType=WinExe and TargetFramework=net5-windows10.0.17763.0
                    // Using 'TargetFramework=net5-windows10.0.17763.0' the framework 'Microsoft.Windows.SDK.NET' is also included as project dependency.
                    // The framework 'Microsoft.Windows.SDK.NET' includes all the WPF(PresentationFramework.dll) and WinForm(System.Windows.Forms.dll) assemblies to the project.

                    var useEmbeddedBrowser = !UserPreferences.Current.UseSystemBrowserForAuthentication;
                    var parameterBuilder = PublicClient.AcquireTokenInteractive(scopes)
                        .WithExtraQueryParameters(MicrosoftAccountOnlyQueryParameter)
                        .WithUseEmbeddedWebView(useEmbeddedBrowser)
                        .WithPrompt(Prompt.SelectAccount) // Force a sign-in (Prompt.SelectAccount), as the MSAL web browser might contain cookies for the current user and we don't necessarily want to re-sign-in the same user                         
                        .WithLoginHint(loginHint)
                        .WithClaims(ex.Claims);

                    if (useEmbeddedBrowser)
                    {
                        var mainwindowHwnd = ProcessHelper.GetCurrentProcessMainWindowHandle();
                        parameterBuilder.WithParentActivityOrWindow(mainwindowHwnd);
                    }

                    var authenticationResult = await parameterBuilder.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                    return authenticationResult;
                }
                catch (MsalException) // ex.ErrorCode => Microsoft.Identity.Client.MsalError
                {
                    throw;
                }
            }
        }

        private async Task EnsureInitializedAsync()
        {
            if (_publicClient is null)
            {
                await _publicClientSemaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_publicClient is null)
                    {
                        await _pbicloudSettings.InitializeAsync().ConfigureAwait(false);

                        _publicClient = MsalHelper.CreatePublicClientApplication(_pbicloudSettings.CloudEnvironment);
                    }
                }
                finally
                {
                    _publicClientSemaphore.Release();
                }
            }
        }
    }
}
