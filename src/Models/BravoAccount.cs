namespace Sqlbi.Bravo.Models
{
    using Microsoft.Identity.Client;
    using System.Text.Json.Serialization;

    public interface IBravoAccount
    {
        string Identifier { get; set; }

        string UserPrincipalName { get; set; }

        string Username { get; set; }
    }

    internal sealed class BravoAccount : IBravoAccount
    {
        /// <summary>
        /// Unique identifier for the account
        /// </summary>
        [JsonPropertyName("id")]
        public string Identifier { get; set; }

        /// <summary>
        /// User name in UserPrincipalName (UPN) format - e.g. john.doe@contoso.com
        /// </summary>
        [JsonPropertyName("upn")]
        public string UserPrincipalName { get; set; }

        /// <summary>
        /// Displayable user name (not guaranteed to be unique, it is mutable)
        /// </summary>
        [JsonPropertyName("username")]
        public string Username { get; set; }

        public BravoAccount(AuthenticationResult authenticationResult)
        {
            Identifier = authenticationResult.Account.HomeAccountId.Identifier;
            UserPrincipalName = authenticationResult.Account.Username;
            Username = authenticationResult.ClaimsPrincipal.FindFirst((claim) => claim.Type == "name")?.Value ?? "<Unknown>";
        }
    }
}
