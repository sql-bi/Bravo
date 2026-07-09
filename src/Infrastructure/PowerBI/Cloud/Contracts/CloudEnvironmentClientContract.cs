namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Contracts
{
    using System.Text.Json.Serialization;

    [DebuggerDisplay("{Name}")]
    internal sealed class CloudEnvironmentClientContract
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("appId")]
        public string AppId { get; set; } = null!;

        [JsonPropertyName("redirectUri")]
        public string RedirectUri { get; set; } = null!;
    }

    internal static class CloudEnvironmentClientContractExtension
    {
        public static bool IsPowerBIDesktop(this CloudEnvironmentClientContract client)
            => client.Name.Equals("powerbi-desktop", StringComparison.Ordinal);
    }
}