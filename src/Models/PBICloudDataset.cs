using System;
using System.Text.Json.Serialization;

#nullable disable

namespace Sqlbi.Bravo.Models
{
    public class PBICloudDataset
    {
        [JsonPropertyName("workspaceId")]
        public string WorkspaceId { get; set; }

        [JsonPropertyName("workspaceName")]
        public string WorkspaceName { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("serverName")]
        public string ServerName { get; set; }

        [JsonPropertyName("databaseName")]
        public string DatabaseName { get; set; }

        [JsonPropertyName("name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("owner")]
        public string Owner { get; set; }

        [JsonPropertyName("refreshed")]
        public DateTime? Refreshed { get; set; }

        [JsonPropertyName("endorsement")]
        public PBICloudDatasetEndorsement Endorsement { get; set; }
    }

    public enum PBICloudDatasetEndorsement
    {
        [JsonPropertyName("None")]
        None = 0,

        [JsonPropertyName("Promoted")]
        Promoted = 1,

        [JsonPropertyName("Certified")]
        Certified = 2,
    }
}
