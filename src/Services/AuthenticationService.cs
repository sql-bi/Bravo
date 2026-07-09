namespace Sqlbi.Bravo.Services
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Authentication;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Configuration;

    public interface IAuthenticationService
    {
        Task<IReadOnlyList<CloudEnvironment>> GetEnvironmentsAsync(string email, CancellationToken cancellationToken);

        Task<AuthenticatedSession?> EnsureSignedInAsync(CancellationToken cancellationToken);

        Task<AuthenticatedSession> SignInAsync(string email, CloudEnvironment environment, CancellationToken cancellationToken);

        Task SignOutAsync(CancellationToken cancellationToken);
    }

    internal class AuthenticationService(
        ICloudAuthenticationService cloudAuthenticationService,
        ICloudConfigurationService cloudConfigurationService) : IAuthenticationService
    {
        private readonly ICloudAuthenticationService _cloudAuthenticationService = cloudAuthenticationService;
        private readonly ICloudConfigurationService _cloudConfigurationService = cloudConfigurationService;

        public async Task<IReadOnlyList<CloudEnvironment>> GetEnvironmentsAsync(string email, CancellationToken cancellationToken)
        {
            return await _cloudConfigurationService.DiscoverEnvironmentsAsync(email, cancellationToken);
        }

        public async Task<AuthenticatedSession?> EnsureSignedInAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _cloudAuthenticationService.EnsureSignedInAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw new BravoException(BravoProblem.SignInMsalTimeoutExpired);
            }
        }

        public async Task<AuthenticatedSession> SignInAsync(string email, CloudEnvironment environment, CancellationToken cancellationToken)
        {
            try
            {
                return await _cloudAuthenticationService.SignInAsync(email, environment, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw new BravoException(BravoProblem.SignInMsalTimeoutExpired);
            }
        }

        public async Task SignOutAsync(CancellationToken cancellationToken)
        {
            await _cloudAuthenticationService.SignOutAsync(cancellationToken);
        }
    }
}
