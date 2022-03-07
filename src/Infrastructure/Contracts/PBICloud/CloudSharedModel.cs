namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System;
    using System.Text.Json.Serialization;

    public sealed class CloudSharedModel
    {
        [JsonPropertyName("modelId")]
        public long ModelId { get; set; }

        [JsonPropertyName("workspaceId")]
        public long WorkspaceId { get; set; }

        [JsonPropertyName("workspaceObjectId")]
        public string? WorkspaceObjectId { get; set; }

        [JsonPropertyName("workspaceName")]
        public string? WorkspaceName { get; set; }

        [JsonPropertyName("workspaceType")]
        public CloudSharedModelWorkspaceType WorkspaceType { get; set; }

        [JsonPropertyName("permissions")]
        public CloudPermissions Permissions { get; set; }

        [JsonPropertyName("model")]
        public CloudModel? Model { get; set; }

        [JsonPropertyName("galleryItem")]
        public CloudOrganizationalGalleryItem? GalleryItem { get; set; }

        //[JsonPropertyName("artifactInformationProtection")]
        //public CloudArtifactInformationProtection ArtifactInformationProtection { get; set; }

        [JsonPropertyName("snapshotId")]
        public long? SnapshotId { get; set; }

        [JsonPropertyName("lastVisitedTimeUTC")]
        public DateTime? LastVisitedTimeUTC { get; set; }

        [JsonPropertyName("__objectId")]
        public string? ObjectId
        {
            get
            {
                if (IsOnPersonalWorkspace)
                {
                    return CloudWorkspace.PersonalWorkspaceId;
                }

                return WorkspaceObjectId;
            }
        }

        [JsonPropertyName("__isOnPersonalWorkspace")]
        public bool IsOnPersonalWorkspace => WorkspaceType == CloudSharedModelWorkspaceType.PersonalGroup;
    }
}
