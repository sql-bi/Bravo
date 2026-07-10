namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Authentication
{
    using Msal = Microsoft.Identity.Client;

    /// <summary>
    /// Bravo-owned snapshot of an MSAL authentication result, decoupled from <see cref="Msal.AuthenticationResult"/>.
    /// </summary>
    [DebuggerDisplay("{Email} ({Name})")]
    public sealed class AuthenticationResult
    {
        private static readonly TimeSpan ExpirationBuffer = TimeSpan.FromSeconds(30);

        private AuthenticationResult(
            string accessToken, DateTimeOffset expiresOn, string tenantId, string userId, string identifier, string email, string name)
        {
            AccessToken = accessToken;
            ExpiresOn = expiresOn;
            TenantId = tenantId;
            UserId = userId;
            Identifier = identifier;
            Email = email;
            Name = name;
        }

        public string AccessToken { get; }
        public DateTimeOffset ExpiresOn { get; }
        public string TenantId { get; }
        public string UserId { get; }
        public string Identifier { get; }
        public string Email { get; }
        public string Name { get; }
        public bool IsExpired => ExpiresOn < DateTimeOffset.UtcNow.Add(ExpirationBuffer);

        public static AuthenticationResult From(Msal.AuthenticationResult msalResult) => new(
            accessToken: msalResult.AccessToken,
            expiresOn: msalResult.ExpiresOn,
            tenantId: msalResult.TenantId,
            userId: msalResult.UniqueId,
            identifier: msalResult.Account.HomeAccountId.Identifier,
            email: msalResult.Account.Username,
            name: msalResult.ClaimsPrincipal.FindFirst((c) => c.Type == "name")?.Value ?? string.Empty);
    }
}
