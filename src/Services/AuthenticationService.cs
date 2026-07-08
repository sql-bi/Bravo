namespace Sqlbi.Bravo.Services
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Services.PowerBI;

    public interface IAuthenticationService
    {
        CloudEnvironment PBICloudEnvironment { get; }

        PBICloudAuthenticationResult PBICloudAuthentication { get; }

        Task<bool> IsPBICloudSignInRequiredAsync(CancellationToken cancellationToken);

        Task PBICloudSignInAsync(string email, CloudEnvironment environment, CancellationToken cancellationToken);

        Task PBICloudSignOutAsync(CancellationToken cancellationToken);
    }

    internal class AuthenticationService : IAuthenticationService
    {
        private readonly IPBICloudAuthenticationService _pbicloudAuthenticationService;

        public AuthenticationService(IPBICloudAuthenticationService pbicloudAuthenticationService)
        {
            _pbicloudAuthenticationService = pbicloudAuthenticationService;
        }

        public CloudEnvironment PBICloudEnvironment
        {
            get
            {
                BravoUnexpectedException.ThrowIfNull(_pbicloudAuthenticationService.CurrentEnvironment);
                return _pbicloudAuthenticationService.CurrentEnvironment;
            }
        }

        public PBICloudAuthenticationResult PBICloudAuthentication
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
                await PBICloudSignInAsync(authentication.Account.Email, environment, cancellationToken).ConfigureAwait(false);
            }

            return false;
        }

        public async Task PBICloudSignInAsync(string email, CloudEnvironment environment, CancellationToken cancellationToken)
        {
            try
            {
                await _pbicloudAuthenticationService.SignInAsync(email, environment, cancellationToken);

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