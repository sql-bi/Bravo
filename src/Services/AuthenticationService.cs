namespace Sqlbi.Bravo.Services
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Models;
    using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IAuthenticationService
    {
        IPBICloudEnvironment PBICloudEnvironment { get; }

        IAuthenticationResult PBICloudAuthentication { get; }

        Task<bool> IsPBICloudSignInRequiredAsync(CancellationToken cancellationToken);

        Task<IEnumerable<IPBICloudEnvironment>> GetPBICloudEnvironmentsAsync(string userPrincipalName, CancellationToken cancellationToken);

        Task PBICloudSignInAsync(string userPrincipalName, IPBICloudEnvironment environment, CancellationToken cancellationToken);

        Task PBICloudSignOutAsync(CancellationToken cancellationToken);
    }

    internal class AuthenticationService : IAuthenticationService
    {
        private readonly IPBICloudAuthenticationService _pbicloudAuthenticationService;

        public AuthenticationService(IPBICloudAuthenticationService pbicloudAuthenticationService)
        {
            _pbicloudAuthenticationService = pbicloudAuthenticationService;
        }

        public IPBICloudEnvironment PBICloudEnvironment
        {
            get
            {
                BravoUnexpectedException.ThrowIfNull(_pbicloudAuthenticationService.CurrentEnvironment);
                return _pbicloudAuthenticationService.CurrentEnvironment;
            }
        }

        public IAuthenticationResult PBICloudAuthentication
        {
            get
            {
                BravoUnexpectedException.ThrowIfNull(_pbicloudAuthenticationService.CurrentAuthentication);
                return _pbicloudAuthenticationService.CurrentAuthentication;
            }
        }

        public async Task<bool> IsPBICloudSignInRequiredAsync(CancellationToken cancellationToken)
        {
            var authentication = _pbicloudAuthenticationService.CurrentAuthentication;
            var environment = _pbicloudAuthenticationService.CurrentEnvironment;

            if (authentication is null || environment is null)
            {
                return true;
            }

            if (authentication.IsExpired)
            {
                await PBICloudSignInAsync(authentication.Account.UserPrincipalName, environment, cancellationToken).ConfigureAwait(false);
            }

            return false;
        }

        public async Task<IEnumerable<IPBICloudEnvironment>> GetPBICloudEnvironmentsAsync(string userPrincipalName, CancellationToken cancellationToken)
        {
            var environments = await _pbicloudAuthenticationService.GetEnvironmentsAsync(userPrincipalName, cancellationToken).ConfigureAwait(false);
            return environments;
        }

        public async Task PBICloudSignInAsync(string userPrincipalName, IPBICloudEnvironment environment, CancellationToken cancellationToken)
        {
            try
            {
                await _pbicloudAuthenticationService.SignInAsync(userPrincipalName, environment, cancellationToken).ConfigureAwait(false);

                BravoUnexpectedException.Assert(_pbicloudAuthenticationService.CurrentAuthentication is not null);
                BravoUnexpectedException.Assert(_pbicloudAuthenticationService.CurrentEnvironment is not null);
            }
            catch (OperationCanceledException)
            {
                throw new BravoException(BravoProblem.SignInMsalTimeoutExpired);
            }
        }

        public async Task PBICloudSignOutAsync(CancellationToken cancellationToken)
        {
            await _pbicloudAuthenticationService.SignOutAsync(cancellationToken).ConfigureAwait(false);
            
            BravoUnexpectedException.Assert(_pbicloudAuthenticationService.CurrentAuthentication is null);
            BravoUnexpectedException.Assert(_pbicloudAuthenticationService.CurrentEnvironment is null);
        }
    }
}