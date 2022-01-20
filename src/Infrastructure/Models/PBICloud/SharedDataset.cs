#nullable disable

using System;

namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    /// <summary>
    /// v201901 - metadata/v201901/gallery/sharedDatasets
    /// </summary>
    public class SharedDataset
    {
        public long ModelId { get; set; }

        public long WorkspaceId { get; set; }

        public string WorkspaceName { get; set; }

        public SharedDatasetWorkspaceType WorkspaceType { get; set; }

        public SharedDatasetPermissions Permissions { get; set; }

        public SharedDatasetModel Model { get; set; }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public SharedDatasetGalleryItem? GalleryItem { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        public string WorkspaceObjectId { get; set; }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public string? SharedFromEnterpriseCapacitySku { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        public DateTime LastVisitedTimeUTC { get; set; }

        public bool IsOnPersonalWorkspace => WorkspaceType == SharedDatasetWorkspaceType.PersonalGroup;
    }
}
