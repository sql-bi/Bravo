using Microsoft.AspNetCore.Hosting;
using Microsoft.Identity.Client;
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
        AuthenticationResult? CurrentAuthentication { get; }

        Task AcquireTokenAsync(TimeSpan cancelAfter, string? identifier = default);

        Task ClearTokenCacheAsync();
    }

    internal class PBICloudAuthenticationService : IPBICloudAuthenticationService
    {
        private const string MicrosoftAccountOnlyQueryParameter = "msafed=0";

        // AuthorityUri     "https://login.microsoftonline.com/common"
        // ClientId         "ea0616ba-638b-4df5-95b9-636659ae5121"
        // EndpointUri  "https://api.powerbi.com"
        // Name         "Public"
        // RedirectUri      "https://login.microsoftonline.com/common/oauth2/nativeclient"
        // ResourceUri  "https://analysis.windows.net/powerbi/api"
        // Scopes       { "https://analysis.windows.net/powerbi/api/.default" }

        private static readonly PublicClientApplicationOptions _options = new()
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "common",
            ClientId = "ea0616ba-638b-4df5-95b9-636659ae5121",
            RedirectUri = "https://login.microsoftonline.com/common/oauth2/nativeclient"
        };

        private static readonly string[] _scopes =
        {
            "https://analysis.windows.net/powerbi/api/.default"
        };

        private static readonly IPublicClientApplication _application;
        private static readonly SemaphoreSlim _tokenSemaphore = new(1);

        static PBICloudAuthenticationService()
        {
            _application = PublicClientApplicationBuilder.Create(_options.ClientId)
                .WithAuthority($"{ _options.Instance }{ _options.TenantId }")
                .WithDefaultRedirectUri()
                .Build();

            TokenCacheHelper.EnableSerialization(_application.UserTokenCache);
        }

        private readonly IWebHostEnvironment _environment;
        private readonly CustomWebViewOptions _customSystemWebViewOptions;

        public PBICloudAuthenticationService(IWebHostEnvironment environment)
        {
            _environment = environment;
            _customSystemWebViewOptions = new CustomWebViewOptions(_environment.WebRootPath);
        }

        public AuthenticationResult? CurrentAuthentication { get; private set; }

        /// <summary>
        /// Removes all account information from MSAL's token cache, removes app-only (not OS-wide) and does not affect the browser cookies
        /// </summary>
        public async Task ClearTokenCacheAsync()
        {
            await _tokenSemaphore.WaitAsync();
            try
            {
                CurrentAuthentication = null;

                var accounts = (await _application.GetAccountsAsync().ConfigureAwait(false)).ToArray();

                foreach (var account in accounts)
                    await _application.RemoveAsync(account).ConfigureAwait(false);
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        public async Task AcquireTokenAsync(TimeSpan cancelAfter, string? identifier = null)
        {
            await _tokenSemaphore.WaitAsync();
            try
            {
                using var cancellationTokenSource = new CancellationTokenSource(cancelAfter);

                var authenticationResult = await InternalAcquireTokenAsync(cancellationTokenSource.Token, identifier).ConfigureAwait(false);
                CurrentAuthentication = authenticationResult;

                //var impersonateTask = System.Security.Principal.WindowsIdentity.RunImpersonatedAsync(Microsoft.Win32.SafeHandles.SafeAccessTokenHandle.InvalidHandle, async () =>
                //{
                //    _authenticationResult = await InternalAcquireTokenAsync(cancellationTokenSource.Token, identifier);
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
        private async Task<AuthenticationResult> InternalAcquireTokenAsync(CancellationToken cancellationToken, string? identifier = null)
        {
            // Use account used to signed-in in Windows (WAM). WAM will always get an account in the cache.
            // So if we want to have a chance to select the accounts interactively, we need to force the non-account
            //identifier = PublicClientApplication.OperatingSystemAccount;

            // Use one of the Accounts known by Windows (WAM), if a null account identifier is provided then force WAM to display the dialog with the accounts
            var account = await _application.GetAccountAsync(identifier).ConfigureAwait(false);
            
            try
            {
                // Try to acquire an access token from the cache, if UI interaction is required, MsalUiRequiredException will be thrown.
                var authenticationResult = await _application.AcquireTokenSilent(_scopes, account).ExecuteAsync(cancellationToken).ConfigureAwait(false);
                return authenticationResult;
            }
            catch (MsalUiRequiredException /* murex */)
            {
                try
                {
                    var builder = _application.AcquireTokenInteractive(_scopes)
                        .WithExtraQueryParameters(MicrosoftAccountOnlyQueryParameter);

                    //.WithAccount(account)
                    //.WithClaims(murex.Claims)
                    //.WithPrompt(Prompt.SelectAccount) // Force a sign-in (Prompt.SelectAccount), as the MSAL web browser might contain cookies for the current user and we don't necessarily want to re-sign-in the same user 

                    if (_application.IsEmbeddedWebViewAvailable())
                    {
                        Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA);
                        Debug.Assert(System.Windows.Forms.Application.MessageLoop == false);

                        var parentWindowHandle = Process.GetCurrentProcess().MainWindowHandle;

                        // Requires VS project OutputType=WinExe and TargetFramework=net5-windows10.0.17763.0
                        builder = builder.WithUseEmbeddedWebView(useEmbeddedWebView: true)
                            .WithParentActivityOrWindow(parentWindowHandle); // used to center embedded wiew on the parent window
                    }
                    else
                    {
                        // If for some reason the EmbeddedWebView is not available than fall back to the SystemWebView
                        builder = builder.WithUseEmbeddedWebView(useEmbeddedWebView: false)
                            .WithSystemWebViewOptions(_customSystemWebViewOptions); // TODO: configure html files
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
    }
}
