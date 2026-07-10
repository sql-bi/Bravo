namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Authentication
{
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud;

    /// <summary>
    /// Represents an authenticated session with the Power BI cloud service,
    /// containing the authentication result and the associated cloud environment.
    /// </summary>
    public sealed class AuthenticatedSession(AuthenticationResult authenticationResult, CloudEnvironment environment)
    {
        public AuthenticationResult AuthenticationResult { get; } = authenticationResult;

        public CloudEnvironment Environment { get; } = environment;
    }
}
