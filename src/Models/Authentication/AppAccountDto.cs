using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models.Authentication
{
    public sealed record AppAccountDto(
        [Required] [property: JsonPropertyName("id")] string Identifier,
        [Required] string Email,
        [Required] string Username);

    internal static class AppAccountDtoMappingExtensions
    {
        internal static AppAccountDto ToDto(this AppAccount account) => new(
            account.Identifier,
            account.Email,
            account.Username);
    }
}
