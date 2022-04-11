namespace Sqlbi.Bravo.Infrastructure.Services.PowerBI
{
    using Microsoft.Identity.Client;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Models;
    using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IPBICloudAuthenticationService
    {
        IAuthenticationResult? CurrentAuthentication { get; }

        IPBICloudEnvironment? CurrentEnvironment { get; }

        Task<IEnumerable<IPBICloudEnvironment>> GetEnvironmentsAsync(string userPrincipalName, CancellationToken cancellationToken);

        Task SignInAsync(string userPrincipalName, IPBICloudEnvironment environment, CancellationToken cancellationToken);

        Task SignOutAsync(CancellationToken cancellationToken);
    }

    internal class PBICloudAuthenticationService : IPBICloudAuthenticationService, IDisposable
    {
        private readonly IPBICloudSettingsService _pbicloudSettings;
        private readonly SemaphoreSlim _authenticationSemaphore = new(1, 1);

        public PBICloudAuthenticationService(IPBICloudSettingsService pbicloudSetting)
        {
            _pbicloudSettings = pbicloudSetting;
        }

        public IAuthenticationResult? CurrentAuthentication { get; private set; }

        public IPBICloudEnvironment? CurrentEnvironment { get; private set; }

        public async Task<IEnumerable<IPBICloudEnvironment>> GetEnvironmentsAsync(string userPrincipalName, CancellationToken cancellationToken)
        {
            var environments = await _pbicloudSettings.GetEnvironmentsAsync(userPrincipalName, cancellationToken).ConfigureAwait(false);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudAuthenticationService) }.{ nameof(GetEnvironmentsAsync) }", JsonSerializer.Serialize(environments));

            return environments;
        }

        public async Task SignInAsync(string userPrincipalName, IPBICloudEnvironment environment, CancellationToken cancellationToken)
        {
            await _authenticationSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cancellationTokenSource.CancelAfter(AppEnvironment.MSALSignInTimeout);

                var previousAuthentication = CurrentAuthentication;
                var previousEnvironment = CurrentEnvironment;

                CurrentAuthentication = await AcquireTokenAsync(userPrincipalName, environment, cancellationTokenSource.Token).ConfigureAwait(false);
                CurrentEnvironment = environment;

                var environmentChanged = !CurrentEnvironment.Equals(previousEnvironment);
                var authenticationChanged = !CurrentAuthentication.Equals(previousAuthentication);
                if (authenticationChanged || environmentChanged)
                {
                    var tenantCluster = await _pbicloudSettings.GetTenantClusterAsync(CurrentAuthentication.AccessToken, cancellationToken).ConfigureAwait(false);
                    CurrentEnvironment.ClusterEndpoint = tenantCluster.FixedClusterUri;
                }
            }
            finally
            {
                _authenticationSemaphore.Release();
            }
        }

        public async Task SignOutAsync(CancellationToken cancellationToken)
        {
            await _authenticationSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                BravoUnexpectedException.ThrowIfNull(CurrentEnvironment);

                await MsalHelper.ClearTokenCacheAsync(CurrentEnvironment);

                CurrentAuthentication = null;
                CurrentEnvironment = null;
            }
            finally
            {
                _authenticationSemaphore.Release();
            }
        }

        private async Task<IAuthenticationResult> AcquireTokenAsync(string userPrincipalName, IPBICloudEnvironment environment, CancellationToken cancellationToken)
        {
            // TODO: Acquire a token using WAM https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-desktop-acquire-token-wam
            // TODO: Acquire a token using integrated Windows authentication https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-desktop-acquire-token-integrated-windows-authentication
            try
            {
                var authenticationResult = await MsalHelper.AcquireTokenSilentAsync(userPrincipalName, environment, cancellationToken).ConfigureAwait(false);
                return authenticationResult;

                //if (UserPreferences.Current.Experimental?.UseIntegratedWindowsAuthenticationSso == true)
                //{
                //    var authenticationResult = await MsalHelper.AcquireTokenByIntegratedWindowsAuthAsync(environment, cancellationToken).ConfigureAwait(false);
                //    return authenticationResult;
                //}
            }
            catch (MsalUiRequiredException msalUiRequiredException)
            {
                var authenticationResult = await MsalHelper.AcquireTokenInteractiveAsync(userPrincipalName, environment, msalUiRequiredException.Claims, cancellationToken).ConfigureAwait(false);
                return authenticationResult;

            }
            catch (MsalServiceException msalServiceException)
            {
                var authenticationResult = await MsalHelper.AcquireTokenInteractiveAsync(userPrincipalName, environment, msalServiceException.Claims, cancellationToken).ConfigureAwait(false);
                return authenticationResult;
            }
            catch (MsalClientException)
            {
                //if (UserPreferences.Current.Experimental?.UseIntegratedWindowsAuthenticationSso == true)
                //{
                //    var authenticationResult = await MsalHelper.AcquireTokenInteractiveAsync(environment, claims: null, cancellationToken).ConfigureAwait(false);
                //    return authenticationResult;
                //}

                throw;
            }
        }

        #region IDisposable

        public void Dispose()
        {
            _authenticationSemaphore.Dispose();
        }

        #endregion
    }
}
