namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class PBICloudAuthenticationRequest
    {
        [Required]
        [JsonPropertyName("userPrincipalName")]
        public string? UserPrincipalName { get; set; }

        [Required]
        [JsonPropertyName("environment")]
        public PBICloudEnvironment? Environment { get; set; }
    }

}
