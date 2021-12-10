#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    public class SharedDataset
    {
        public long ModelId { get; set; }

        public long WorkspaceId { get; set; }

        public string WorkspaceObjectId { get; set; }

        public string WorkspaceName { get; set; }

        public SharedDatasetWorkspaceType WorkspaceType { get; set; }

        public SharedDatasetPermissions Permissions { get; set; }

        public SharedDatasetModel Model { get; set; }

        public SharedDatasetGalleryItem? GalleryItem { get; set; }
    }
}
