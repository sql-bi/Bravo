namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    using Microsoft.Identity.Client;
    using Sqlbi.Bravo.Models;

    [DebuggerDisplay($"{{{ nameof(GetDebuggerDisplay) }(),nq}}")]
    public sealed class PBICloudAuthenticationResult
    {
        private readonly AuthenticationResult _authenticationResult;

        public PBICloudAuthenticationResult(AuthenticationResult authenticationResult)
        {
            _authenticationResult = authenticationResult;
            Account = new AppAccount(authenticationResult);
        }

        public bool IsExpired => _authenticationResult.ExpiresOn < DateTimeOffset.UtcNow.AddMinutes(1);

        public string AccessToken => _authenticationResult.AccessToken;

        public AppAccount Account { get; private set; }

        private string GetDebuggerDisplay()
        {
            return _authenticationResult.Account.Username;
        }
    }
}
