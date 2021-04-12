using Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models;
using System;

namespace Sqlbi.Bravo.Client.PowerBI.PowerBICloud
{
    internal class PowerBICloudSharedDataset
    {
        public Guid WorkspaceId { get; init; }

        public string WorkspaceName { get; init; }

        public WorkspaceType WorkspaceType { get; init; }

        public WorkspaceCapacitySkuType WorkspaceCapacitySkuType { get; init; }

        public SharedDatasetPermissions Permissions { get; init; }

        public SharedDatasetModel Model { get; init; }

        public SharedDatasetGalleryItem GalleryItem { get; set; }
    }
}
