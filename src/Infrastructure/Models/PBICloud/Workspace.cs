using System;
using System.Text.Json.Serialization;

#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    public class Workspace
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public string CapacitySku { get; set; }

        public string CapacityUri { get; set; }

        public WorkspaceType GetWorkspaceType() => (WorkspaceType)Enum.Parse(typeof(WorkspaceType), Type!);

        [JsonIgnore]
        public WorkspaceCapacitySkuType CapacitySkuType => (WorkspaceCapacitySkuType)Enum.Parse(typeof(WorkspaceCapacitySkuType), CapacitySku);
    }
}
