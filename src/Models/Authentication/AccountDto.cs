using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Authentication;

namespace Sqlbi.Bravo.Models.Authentication;

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
