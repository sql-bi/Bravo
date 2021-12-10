using System;
using System.Text.Json.Serialization;

#nullable disable

namespace Sqlbi.Bravo.Models
{
    public class PBICloudDataset
    {
        [JsonPropertyName("workspaceId")]
        public long WorkspaceId { get; set; }

        [JsonPropertyName("workspaceName")]
        public string WorkspaceName { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("owner")]
        public string Owner { get; set; }

        [JsonPropertyName("refreshed")]
        public DateTimeOffset? Refreshed { get; set; }

        [JsonPropertyName("endorsement")]
        public PBICloudDatasetEndorsement Endorsement { get; set; }
    }

    public enum PBICloudDatasetEndorsement
    {
        None = 0,

        Promoted = 1,

        Certified = 2,
    }
}
