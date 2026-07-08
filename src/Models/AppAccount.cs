namespace Sqlbi.Bravo.Models
{
    using Microsoft.Identity.Client;

    public sealed class AppAccount
    {
        /// <summary>
        /// Unique identifier for the account
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// User name in UserPrincipalName (UPN) format - e.g. john.doe@contoso.com
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Displayable user name (not guaranteed to be unique, it is mutable)
        /// </summary>
        public string Username { get; set; }

        public AppAccount(AuthenticationResult authenticationResult)
        {
            Identifier = authenticationResult.Account.HomeAccountId.Identifier;
            Email = authenticationResult.Account.Username;
            Username = authenticationResult.ClaimsPrincipal.FindFirst((claim) => claim.Type == "name")?.Value ?? "<Unknown>";
        }
    }
}
