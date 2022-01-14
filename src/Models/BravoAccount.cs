using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class BravoAccount
    {
        /// <summary>
        /// Unique identifier for the account
        /// </summary>
        [JsonPropertyName("id")]
        public string? Identifier { get; set; }

        /// <summary>
        /// User name in UserPrincipalName (UPN) format - e.g. john.doe@contoso.com
        /// </summary>
        [JsonPropertyName("upn")]
        public string? UserPrincipalName { get; set; }

        /// <summary>
        /// Displayable user name (not guaranteed to be unique, it is mutable)
        /// </summary>
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        /// <summary>
        /// User profile picture as base64 encoded image [data:image/jpeg;base64,...]
        /// </summary>
        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }
    }
}
