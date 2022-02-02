namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    using System.Text.Json.Serialization;

    public sealed class CloudUser
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("givenName")]
        public string? GivenName { get; set; }

        [JsonPropertyName("familyName")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("emailAddress")]
        public string? EmailAddress { get; set; }
    }
}
