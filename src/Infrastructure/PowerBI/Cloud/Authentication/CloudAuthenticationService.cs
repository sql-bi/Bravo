namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Authentication
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Configuration;
    using Sqlbi.Bravo.Models;

    public interface ICloudAuthenticationService
    {
        Task<AuthenticatedSession> SignInAsync(string email, CloudEnvironment environment, CancellationToken cancellationToken);

        Task<AuthenticatedSession?> EnsureSignedInAsync(CancellationToken cancellationToken);

        Task SignOutAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Orchestrates PBI Cloud sign-in/out and holds session state, delegating all MSAL work to <see cref="ICloudAuthenticationClient"/>.
    /// </summary>
    internal class CloudAuthenticationService(
        ICloudAuthenticationClient cloudAuthenticationClient,
        ICloudConfigurationService cloudConfigurationService) : ICloudAuthenticationService, IDisposable
    {
        private readonly ICloudAuthenticationClient _cloudAuthenticationClient = cloudAuthenticationClient;
        private readonly ICloudConfigurationService _cloudConfigurationService = cloudConfigurationService;
        private readonly SemaphoreSlim _authenticationSemaphore = new(1, 1);

        private AuthenticatedSession? _currentSession;

        public async Task<AuthenticatedSession> SignInAsync(string email, CloudEnvironment environment, CancellationToken cancellationToken)
        {
            await _authenticationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (AppEnvironment.IsDiagnosticLevelVerbose)
                    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{nameof(CloudAuthenticationService)}.{nameof(SignInAsync)}", JsonSerializer.Serialize(environment));

                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(2));

                var authenticationResult = await _cloudAuthenticationClient.AcquireTokenAsync(environment, email, cancellationTokenSource.Token);
                var clusterUri = await _cloudConfigurationService.ResolveTenantClusterUriAsync(environment, authenticationResult.AccessToken, cancellationTokenSource.Token);

                var newEnvironment = environment with { ClusterUri = clusterUri };
                var newSession = new AuthenticatedSession(authenticationResult, newEnvironment);

                return _currentSession = newSession;
            }
            finally
            {
                _authenticationSemaphore.Release();
            }
        }

        public async Task<AuthenticatedSession?> EnsureSignedInAsync(CancellationToken cancellationToken)
        {
            var session = _currentSession;
            if (session is null)
                return null;

            if (session.AuthenticationResult.IsExpired)
                return await SignInAsync(session.AuthenticationResult.Email, session.Environment, cancellationToken);

            return session;
        }

        public async Task SignOutAsync(CancellationToken cancellationToken)
        {
            await _authenticationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_currentSession is not null)
                {
                    await _cloudAuthenticationClient.ClearTokenCacheAsync(_currentSession.Environment);
                }

                _currentSession = null;
            }
            finally
            {
                _authenticationSemaphore.Release();
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
