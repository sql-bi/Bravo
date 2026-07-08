namespace Sqlbi.Bravo.Infrastructure.Services.PowerBI
{
    using Microsoft.Identity.Client;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
    using Sqlbi.Bravo.Models;

    public interface IPBICloudAuthenticationService
    {
        PBICloudAuthenticationResult? CurrentAuthentication { get; }

        CloudEnvironment? CurrentEnvironment { get; }

        Task<IEnumerable<CloudEnvironment>> GetEnvironmentsAsync(string email, CancellationToken cancellationToken);

        Task SignInAsync(string email, CloudEnvironment environment, CancellationToken cancellationToken);

        Task SignOutAsync(CancellationToken cancellationToken);
    }

    internal class PBICloudAuthenticationService : IPBICloudAuthenticationService, IDisposable
    {
        private readonly IPBICloudConfigurationService _pbicloudConfiguration;
        private readonly SemaphoreSlim _authenticationSemaphore = new(1, 1);

        public PBICloudAuthenticationService(IPBICloudConfigurationService pbicloudConfiguration)
        {
            _pbicloudConfiguration = pbicloudConfiguration;
        }

        public PBICloudAuthenticationResult? CurrentAuthentication { get; private set; }

        public CloudEnvironment? CurrentEnvironment { get; private set; }

        public async Task<IEnumerable<CloudEnvironment>> GetEnvironmentsAsync(string email, CancellationToken cancellationToken)
        {
            var environments = await _pbicloudConfiguration.DiscoverCloudEnvironmentsAsync(email, cancellationToken);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(PBICloudAuthenticationService) }.{ nameof(GetEnvironmentsAsync) }", JsonSerializer.Serialize(environments));

            return environments;
        }

        public async Task SignInAsync(string email, CloudEnvironment environment, CancellationToken cancellationToken)
        {
            await _authenticationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (AppEnvironment.IsDiagnosticLevelVerbose)
                    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{nameof(PBICloudAuthenticationService)}.{nameof(SignInAsync)}", JsonSerializer.Serialize(environment));

                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(2));

                var authentication = await AcquireTokenAsync(email, environment, cancellationTokenSource.Token);
                var clusterUri = await _pbicloudConfiguration.ResolveTenantClusterUriAsync(environment, authentication.AccessToken, cancellationToken);

                CurrentAuthentication = authentication;
                CurrentEnvironment = environment with { ClusterUri = clusterUri };
            }
            finally
            {
                _authenticationSemaphore.Release();
            }
        }

        public async Task SignOutAsync(CancellationToken cancellationToken)
        {
            await _authenticationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (CurrentEnvironment is not null)
                {
                    await MsalHelper.ClearTokenCacheAsync(CurrentEnvironment);
                }

                CurrentAuthentication = null;
                CurrentEnvironment = null;
            }
            finally
            {
                _authenticationSemaphore.Release();
            }
        }

        private async Task<PBICloudAuthenticationResult> AcquireTokenAsync(string email, CloudEnvironment environment, CancellationToken cancellationToken)
        {
            // TODO: Acquire a token using WAM https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-desktop-acquire-token-wam
            // TODO: Acquire a token using integrated Windows authentication https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-desktop-acquire-token-integrated-windows-authentication
            try
            {
                return await MsalHelper.AcquireTokenSilentAsync(email, environment, cancellationToken);

                //if (UserPreferences.Current.Experimental?.UseIntegratedWindowsAuthenticationSso == true)
                //{
                //    return await MsalHelper.AcquireTokenByIntegratedWindowsAuthAsync(environment, cancellationToken);
                //}
            }
            catch (MsalUiRequiredException msalUiRequiredException)
            {
                return await MsalHelper.AcquireTokenInteractiveAsync(email, environment, msalUiRequiredException.Claims, cancellationToken);

            }
            catch (MsalServiceException msalServiceException)
            {
                return await MsalHelper.AcquireTokenInteractiveAsync(email, environment, msalServiceException.Claims, cancellationToken);
            }
            catch (MsalClientException)
            {
                //if (UserPreferences.Current.Experimental?.UseIntegratedWindowsAuthenticationSso == true)
                //{
                //    return await MsalHelper.AcquireTokenInteractiveAsync(environment, claims: null, cancellationToken);
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
