namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System.Text.Json.Serialization;

    public class TenantCluster
    {
        [JsonPropertyName("FixedClusterUri")]
        public string? FixedClusterUri { get; set; }

        //public string? PrivateLinkFixedClusterUri { get; set; }

        //public string? NewTenantId { get; set; }

        //public string? RuleDescription { get; set; }

        //public int? TTLSeconds { get; set; }

        //public string? TenantId { get; set; }
    }
}