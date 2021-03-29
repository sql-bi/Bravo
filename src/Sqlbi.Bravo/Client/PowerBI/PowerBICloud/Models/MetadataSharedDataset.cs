using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models
{
    internal class MetadataSharedDataset
    {
        public long ModelId { get; set; }

        public long WorkspaceId { get; set; }

        public string WorkspaceObjectId { get; set; }

        public string WorkspaceName { get; set; }

        public MetadataWorkspaceType WorkspaceType { get; set; }

        public MetadataPermissions Permissions { get; set; }

        public MetadataModel Model { get; set; }
    }
}
