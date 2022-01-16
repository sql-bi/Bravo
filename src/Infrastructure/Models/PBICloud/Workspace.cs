using System;

#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    public class Workspace
    {
        /// <summary>
        /// Ignore and treat Workspace.Id as empty if the workspace IsPersonalWorkspace
        /// </summary>
        public string Id { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public string CapacitySku { get; set; }

        public string CapacityUri { get; set; }

        public bool IsV1Workspace => WorkspaceType == WorkspaceType.Group;

        public bool IsPersonalWorkspace => WorkspaceType == WorkspaceType.User;

        public bool IsXmlaEndPointSupported => CapacitySkuType == WorkspaceCapacitySkuType.Premium;

        public WorkspaceType WorkspaceType => (WorkspaceType)Enum.Parse(typeof(WorkspaceType), Type);

        public WorkspaceCapacitySkuType CapacitySkuType => (WorkspaceCapacitySkuType)Enum.Parse(typeof(WorkspaceCapacitySkuType), CapacitySku);
    }
}
