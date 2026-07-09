namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Contracts
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
