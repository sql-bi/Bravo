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
#nullable enable
        public SharedDatasetGalleryItem? GalleryItem { get; set; }
#nullable disable
        public string WorkspaceObjectId { get; set; }
#nullable enable
        public string? SharedFromEnterpriseCapacitySku { get; set; }
#nullable disable
        public DateTime LastVisitedTimeUTC { get; set; }
    }
}
