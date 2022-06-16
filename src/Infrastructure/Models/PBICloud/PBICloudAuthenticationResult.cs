namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    using Microsoft.Identity.Client;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Diagnostics;

    [DebuggerDisplay($"{{{ nameof(GetDebuggerDisplay) }(),nq}}")]
    internal sealed class PBICloudAuthenticationResult : IAuthenticationResult
    {
        private readonly AuthenticationResult _authenticationResult;

        public PBICloudAuthenticationResult(AuthenticationResult authenticationResult)
        {
            _authenticationResult = authenticationResult;
            Account = new BravoAccount(authenticationResult);
        }

        public bool IsExpired => _authenticationResult.ExpiresOn < DateTimeOffset.UtcNow.AddMinutes(1);

        public string AccessToken => _authenticationResult.AccessToken;

        public IBravoAccount Account { get; private set; }

        #region IEquatable

        public override bool Equals(object? obj)
        {
            return Equals(obj as PBICloudAuthenticationResult);
        }

        public bool Equals(PBICloudAuthenticationResult? other)
        {
            return other != null &&
                   Account.Identifier == other.Account.Identifier;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Account.Identifier);
        }

        #endregion

        private string GetDebuggerDisplay()
        {
            return _authenticationResult.Account.Username;
        }
    }
}
