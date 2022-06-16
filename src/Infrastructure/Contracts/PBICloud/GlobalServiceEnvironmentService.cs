namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class GlobalServiceEnvironmentService
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("endpoint")]
        public string? Endpoint { get; set; }

        [JsonPropertyName("resourceId")]
        public string? ResourceId { get; set; }

        [JsonPropertyName("allowedDomains")]
        public IEnumerable<string>? AllowedDomains { get; set; }

        [JsonPropertyName("appId")]
        public string? AppId { get; set; }

        [JsonPropertyName("redirectUri")]
        public string? RedirectUri { get; set; }
    }
}
