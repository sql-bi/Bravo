using Microsoft.AspNetCore.Hosting;
using Microsoft.Identity.Client;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Authentication;
using Sqlbi.Bravo.Infrastructure.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Services
{
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
        private const string MicrosoftAccountOnlyQueryParameter = "msafed=0";

        private readonly CustomWebViewOptions _customSystemWebViewOptions;
        private readonly SemaphoreSlim _tokenSemaphore = new(1);
        private readonly IPBICloudSettingsService _pbisettings;
        private readonly IWebHostEnvironment _environment;
        private readonly object _publicClientLockObj = new();

        private IPublicClientApplication? _publicClient;

        public PBICloudAuthenticationService(IPBICloudSettingsService pbisetting, IWebHostEnvironment environment)
        {
            _pbisettings = pbisetting;
            _environment = environment;

            _customSystemWebViewOptions = new CustomWebViewOptions(_environment.WebRootPath);
        }

        private IPublicClientApplication PublicClient => _publicClient ?? throw new BravoUnexpectedException("_publicClient is null");

        public AuthenticationResult? Authentication { get; private set; }

        public Uri TenantCluster => new(_pbisettings.TenantCluster.FixedClusterUri);

        /// <summary>
        /// Removes all account information from MSAL's token cache, removes app-only (not OS-wide) and does not affect the browser cookies
        /// </summary>
        public async Task ClearTokenCacheAsync()
        {
            await _tokenSemaphore.WaitAsync();
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                Authentication = null;

                var accounts = (await PublicClient.GetAccountsAsync().ConfigureAwait(false)).ToArray();

                foreach (var account in accounts)
                    await PublicClient.RemoveAsync(account).ConfigureAwait(false);
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var cachedAccount = await PublicClient.GetAccountAsync(Authentication?.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            if (cachedAccount is null)
            {
                return false; // Fail-fast here because no cached token is available
            }

            try
            {
                await AcquireTokenAsync(silent: true).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                return false;
            }

            var accountChanged = !cachedAccount.HomeAccountId.Equals(Authentication?.Account.HomeAccountId);
            if (accountChanged)
            {
                throw new BravoUnexpectedException("Account changed after token refresh");
            }

            return true;
        }

        public async Task AcquireTokenAsync(bool silent = false, string? loginHint = null, TimeSpan? timeout = null)
        {
            await _tokenSemaphore.WaitAsync();
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                using var cancellationTokenSource = new CancellationTokenSource();
                if (timeout.HasValue) cancellationTokenSource.CancelAfter(timeout.Value);

                var identifier = Authentication?.Account.HomeAccountId.Identifier;
                var previousAuthentication = Authentication;
                var currentAuthentication = Authentication = await AcquireTokenImplAsync(silent, identifier, loginHint, cancellationTokenSource.Token).ConfigureAwait(false);

                var accountChanged = !currentAuthentication.Account.HomeAccountId.Equals(previousAuthentication?.Account.HomeAccountId);
                if (accountChanged)
                { 
                    await _pbisettings.RefreshAsync(currentAuthentication.AccessToken).ConfigureAwait(false);
                }

                //var impersonateTask = System.Security.Principal.WindowsIdentity.RunImpersonatedAsync(Microsoft.Win32.SafeHandles.SafeAccessTokenHandle.InvalidHandle, async () =>
                //{
                //    _authenticationResult = await AcquireTokenImplAsync(...);
                //});
                //await impersonateTask.ConfigureAwait(false);
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-desktop-acquire-token?tabs=dotnet
        /// </summary>
        private async Task<AuthenticationResult> AcquireTokenImplAsync(bool silent, string? identifier, string? loginHint, CancellationToken cancellationToken)
        {
            Authentication = null;

            // Use account used to signed-in in Windows (WAM). WAM will always get an account in the cache so, if we want to have a chance to select the accounts interactively, we need to force the non-account.
            //identifier = PublicClientApplication.OperatingSystemAccount;

            // Use one of the Accounts known by Windows (WAM), if a null account identifier is provided then force WAM to display the dialog with the accounts
            var account = await PublicClient.GetAccountAsync(identifier).ConfigureAwait(false);

            try
            {
                // Try to acquire an access token from the cache, if UI interaction is required, MsalUiRequiredException will be thrown.
                var authenticationResult = await PublicClient.AcquireTokenSilent(_pbisettings.CloudEnvironment.Scopes, account).ExecuteAsync(cancellationToken).ConfigureAwait(false);
                return authenticationResult;
            }
            catch (MsalUiRequiredException)
            {
                // Re-throw exception if silent-only token acquisition was requested
                if (silent) throw;
                try
                {
                    var builder = PublicClient.AcquireTokenInteractive(_pbisettings.CloudEnvironment.Scopes)
                        .WithExtraQueryParameters(MicrosoftAccountOnlyQueryParameter);

                    //.WithClaims(murex.Claims)
                    //.WithPrompt(Prompt.SelectAccount) // Force a sign-in (Prompt.SelectAccount), as the MSAL web browser might contain cookies for the current user and we don't necessarily want to re-sign-in the same user 

                    if (account is not null)
                        builder.WithAccount(account);
                    else if (loginHint is not null)
                        builder.WithLoginHint(loginHint);

                    if (PublicClient.IsEmbeddedWebViewAvailable())
                    {
                        Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA);
                        Debug.Assert(System.Windows.Forms.Application.MessageLoop == false);

                        var parentWindowHandle = Process.GetCurrentProcess().MainWindowHandle;

                        // *** EmbeddedWebView requirements ***
                        // Requires VS project OutputType=WinExe and TargetFramework=net5-windows10.0.17763.0
                        // Using 'TargetFramework=net5-windows10.0.17763.0' the framework 'Microsoft.Windows.SDK.NET' is also included as project dependency.
                        // The framework 'Microsoft.Windows.SDK.NET' includes all the WPF(PresentationFramework.dll) and WinForm(System.Windows.Forms.dll) assemblies to the project.

                        builder = builder.WithUseEmbeddedWebView(useEmbeddedWebView: true)
                            .WithParentActivityOrWindow(parentWindowHandle); // used to center embedded wiew on the parent window
                    }
                    else
                    {
                        // If for some reason the EmbeddedWebView is not available than fall back to the SystemWebView
                        builder = builder.WithUseEmbeddedWebView(useEmbeddedWebView: false)
                            .WithSystemWebViewOptions(_customSystemWebViewOptions);
                    }

                    var authenticationResult = await builder.ExecuteAsync(cancellationToken).ConfigureAwait(false);
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
                await _pbisettings.InitializeAsync().ConfigureAwait(false);

                lock (_publicClientLockObj) 
                {
                    if (_publicClient is null)
                    {
                        _publicClient = PublicClientApplicationBuilder.Create(_pbisettings.CloudEnvironment.ClientId)
                            .WithAuthority(_pbisettings.CloudEnvironment.Authority)
                            .WithDefaultRedirectUri()
                            .Build();

                        TokenCacheHelper.EnableSerialization(_publicClient.UserTokenCache);
                    }
                }
            }
        }
    }
}
