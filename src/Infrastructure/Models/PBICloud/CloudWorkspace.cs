namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    using System;

    public sealed class CloudWorkspace
    {
        private static readonly string PersonalWorkspaceDefaultId = Guid.Empty.ToString();

        private string? _workspaceObjectId;

        private string? _workspaceName;

        public string? Id 
        {
            get => IsPersonalWorkspace ? PersonalWorkspaceDefaultId : _workspaceObjectId;
            set => _workspaceObjectId = value;
        }

        public string? Name
        {
            get => IsPersonalWorkspace ? null : _workspaceName;
            set => _workspaceName = value;
        }

        public string? Type { get; set; }

        public string? CapacitySku { get; set; }

        public string? WorkspaceObjectId => _workspaceObjectId;

        public bool IsLegacyV1Workspace => WorkspaceType == CloudWorkspaceType.Group;

        public bool IsXmlaEndPointSupported => CapacitySkuType == CloudWorkspaceCapacitySkuType.Premium;

        public bool IsPersonalWorkspace
        {
            get
            {
                if (string.IsNullOrEmpty(_workspaceName))
                {
                    return WorkspaceType == CloudWorkspaceType.User;
                }

                return false;
            }
        }

        public CloudWorkspaceType WorkspaceType => (CloudWorkspaceType)Enum.Parse(typeof(CloudWorkspaceType), Type!); // here we don't expect null, but in case we let the ArgumentNullException arise

        public CloudWorkspaceCapacitySkuType CapacitySkuType => (CloudWorkspaceCapacitySkuType)Enum.Parse(typeof(CloudWorkspaceCapacitySkuType), CapacitySku!); // here we don't expect null, but in case we let the ArgumentNullException arise
    }
}
