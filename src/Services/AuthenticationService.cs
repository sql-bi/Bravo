namespace Sqlbi.Bravo.Services
{
    using Microsoft.Identity.Client;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Threading.Tasks;

    public interface IAuthenticationService
    {
        BravoAccount? Account { get; }

        Uri PBICloudTenantCluster { get; }

        AuthenticationResult PBICloudAuthentication { get; }

        Task<bool> IsPBICloudSignInRequiredAsync();

        Task PBICloudSignInAsync(string? userPrincipalName = null);

        Task PBICloudSignOutAsync();
    }

    internal class AuthenticationService : IAuthenticationService
    {
        private readonly IPBICloudAuthenticationService _pbicloudAuthenticationService;

        public AuthenticationService(IPBICloudAuthenticationService pbicloudAuthenticationService)
        {
            _pbicloudAuthenticationService = pbicloudAuthenticationService;
        }

        public BravoAccount? Account { get; private set; }

        public Uri PBICloudTenantCluster => _pbicloudAuthenticationService.TenantCluster;

        public AuthenticationResult PBICloudAuthentication
        {
            get
            {
                BravoUnexpectedException.ThrowIfNull(_pbicloudAuthenticationService.Authentication);
                return _pbicloudAuthenticationService.Authentication;
            }
        }

        public async Task<bool> IsPBICloudSignInRequiredAsync()
        {
            var refreshSucceeded = await _pbicloudAuthenticationService.RefreshTokenAsync().ConfigureAwait(false);
            if (refreshSucceeded)
            {
                RefreshAccount();
                return false;  // No SignIn required - cached token is valid
            }
            else
            {
                // SignIn required - an interaction is required with the end user of the application, for instance:
                // - no refresh token was in the cache
                // - the user needs to consent or re-sign-in (for instance if the password expired)
                // - the user needs to perform two factor auth
                return true;
            }
        }

        public async Task PBICloudSignInAsync(string? loginHint = null)
        {
            Account = null;

            try
            {
                await _pbicloudAuthenticationService.AcquireTokenAsync(silent: false, loginHint, timeout: AppEnvironment.MSALSignInTimeout).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new BravoException(BravoProblem.SignInMsalTimeoutExpired);
            }

            RefreshAccount();
        }

        public async Task PBICloudSignOutAsync()
        {
            Account = null;

            await _pbicloudAuthenticationService.ClearTokenCacheAsync().ConfigureAwait(false);

            BravoUnexpectedException.Assert(_pbicloudAuthenticationService.Authentication is null);
        }

        private void RefreshAccount()
        {
            var currentAccountChanged = PBICloudAuthentication.Account.HomeAccountId.Identifier.Equals(Account?.Identifier) == false;
            if (currentAccountChanged)
            {
                var account = new BravoAccount
                {
                    Identifier = PBICloudAuthentication.Account.HomeAccountId.Identifier,
                    UserPrincipalName = PBICloudAuthentication.Account.Username,
                    Username = PBICloudAuthentication.ClaimsPrincipal.FindFirst((c) => c.Type == "name")?.Value,
                };

                Account = account;
            }
        }
    }
}