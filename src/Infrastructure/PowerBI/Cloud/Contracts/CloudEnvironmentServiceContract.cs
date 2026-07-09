namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Contracts
{
    using System.Text.Json.Serialization;

    [DebuggerDisplay("{Name}")]
    internal sealed class CloudEnvironmentServiceContract
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = null!;

        [JsonPropertyName("resourceId")]
        public string ResourceId { get; set; } = null!;

        //[JsonPropertyName("allowedDomains")]
        //public string[] AllowedDomains { get; set; } = null!;
    }

    internal static class CloudEnvironmentServiceContractExtension
    {
        public static bool IsAad(this CloudEnvironmentServiceContract service)
            => service.Name.Equals("aad", StringComparison.Ordinal);

        public static bool IsPowerBIBackend(this CloudEnvironmentServiceContract service)
            => service.Name.Equals("powerbi-backend", StringComparison.Ordinal);
    }
}
