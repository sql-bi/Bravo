namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System.Text.Json.Serialization;

    // Sample response from the discover API:
    // Invoke-RestMethod -Method POST -Uri "https://api.powerbi.com/powerbi/globalservice/v202003/environments/discover?client=powerbi-msolap" | ConvertTo-Json -Depth 10

    internal sealed class CloudEnvironmentResponseContract
    {
        [JsonPropertyName("environments")]
        public CloudEnvironmentContract[] Environments { get; set; } = null!;
    }
}
