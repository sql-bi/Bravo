namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System.Text.Json.Serialization;

    public class GlobalServiceEnvironmentClient
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("appId")]
        public string? AppId { get; set; }

        [JsonPropertyName("redirectUri")]
        public string? RedirectUri { get; set; }
    }
}