﻿namespace Sqlbi.Bravo.Infrastructure.Services.PowerBI
{
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

        private readonly static SemaphoreSlim _authenticationSemaphore = new(1, 1);
        private readonly static SemaphoreSlim _publicClientSemaphore = new(1, 1);
        private readonly MsalSystemWebViewOptions _systemWebViewOptions;
        private readonly IPBICloudSettingsService _pbicloudSettings;
        private readonly IWebHostEnvironment _environment;

        private IPublicClientApplication? _publicClient;

        public PBICloudAuthenticationService(IWebHostEnvironment environment, IPBICloudSettingsService pbicloudSetting)
        {
            _environment = environment;
            _pbicloudSettings = pbicloudSetting;

            _systemWebViewOptions = new MsalSystemWebViewOptions(_environment.WebRootPath);
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
            }
            catch (MsalUiRequiredException)
            {
                return false;
            }

            return true;
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

            try
            {
                // Try to acquire an access token from the cache, if UI interaction is required, MsalUiRequiredException will be thrown.
                var authenticationResult = await PublicClient.AcquireTokenSilent(_pbicloudSettings.CloudEnvironment.Scopes, account).ExecuteAsync(cancellationToken).ConfigureAwait(false);
                return authenticationResult;
            }
            catch (MsalUiRequiredException)
            {
                // Re-throw exception if silent-only token acquisition was requested
                if (silentOnly) throw;
                try
                {
                    var builder = PublicClient.AcquireTokenInteractive(_pbicloudSettings.CloudEnvironment.Scopes)
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

                        var parentHwnd = ProcessHelper.GetCurrentProcessMainWindowHandle();

                        // *** EmbeddedWebView requirements ***
                        // Requires VS project OutputType=WinExe and TargetFramework=net5-windows10.0.17763.0
                        // Using 'TargetFramework=net5-windows10.0.17763.0' the framework 'Microsoft.Windows.SDK.NET' is also included as project dependency.
                        // The framework 'Microsoft.Windows.SDK.NET' includes all the WPF(PresentationFramework.dll) and WinForm(System.Windows.Forms.dll) assemblies to the project.

                        builder = builder.WithUseEmbeddedWebView(useEmbeddedWebView: true)
                            .WithParentActivityOrWindow(parentHwnd); // used to center embedded wiew on the parent window
                    }
                    else
                    {
                        // If for some reason the EmbeddedWebView is not available than fall back to the SystemWebView
                        builder = builder.WithUseEmbeddedWebView(useEmbeddedWebView: false)
                            .WithSystemWebViewOptions(_systemWebViewOptions);
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
                await _publicClientSemaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_publicClient is null)
                    {
                        await _pbicloudSettings.InitializeAsync().ConfigureAwait(false);

                        _publicClient = PublicClientApplicationBuilder.Create(_pbicloudSettings.CloudEnvironment.ClientId)
                            .WithAuthority(_pbicloudSettings.CloudEnvironment.Authority)
                            .WithDefaultRedirectUri()
                            .Build();

                        TokenCacheHelper.EnableSerialization(_publicClient.UserTokenCache);
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
