using System;

namespace Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models
{
    internal class Workspace
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public string CapacitySku { get; set; }

        public string CapacityUri { get; set; }

        public WorkspaceType GetWorkspaceType() => (WorkspaceType)Enum.Parse(typeof(WorkspaceType), Type);

        public WorkspaceCapacitySkuType GetWorkspaceCapacitySkuType() => (WorkspaceCapacitySkuType)Enum.Parse(typeof(WorkspaceCapacitySkuType), CapacitySku);
    }
}
