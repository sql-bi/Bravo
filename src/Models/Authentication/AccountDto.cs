using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models.Authentication
{
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Authentication;

    public sealed record AccountDto(
        [Required] [property: JsonPropertyName("id")] string Identifier,
        [Required] string Email,
        [Required] string Username);

    internal static class AccountDtoMappingExtensions
    {
        internal static AccountDto ToDto(this AuthenticationResult authenticationResult) => new(
            authenticationResult.Identifier,
            authenticationResult.Email,
            authenticationResult.Name);
    }
}
