namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System;
    using System.Text.Json.Serialization;

    public sealed class CloudWorkspace
    {
        internal static readonly string PersonalWorkspaceId = Guid.Empty.ToString();

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("capacitySku")]
        public string? CapacitySku { get; set; }

        [JsonPropertyName("capacityObjectId")]
        public string? CapacityObjectId { get; set; }

        [JsonPropertyName("capacityUri")]
        public string? CapacityUri { get; set; }

        [JsonPropertyName("isSharedOnPremium")]
        public bool IsSharedOnPremium { get; set; }

        [JsonPropertyName("__objectId")]
        public string? ObjectId
        {
            get
            {
                if (IsPersonalWorkspace)
                {
                    return PersonalWorkspaceId;
                }

                return Id;
            }
        }

        [JsonPropertyName("__isIsLegacyV1Workspace")]
        public bool IsLegacyV1Workspace => WorkspaceType == CloudWorkspaceType.Group;

        [JsonPropertyName("__isPersonalWorkspace")]
        public bool IsPersonalWorkspace
        {
            get
            {
                if (string.IsNullOrEmpty(Name))
                {
                    return WorkspaceType == CloudWorkspaceType.User;
                }

                return false;
            }
        }

        [JsonPropertyName("__isPremiumCapacity")]
        public bool IsPremiumCapacity
        {
            get
            {
                if (CapacitySkuType == CloudWorkspaceCapacitySkuType.Premium)
                {
                    return !IsSharedOnPremium;
                }

                return false;
            }
        }

        [JsonPropertyName("__workspaceType")]
        public CloudWorkspaceType WorkspaceType => (CloudWorkspaceType)Enum.Parse(typeof(CloudWorkspaceType), Type!); // here we don't expect null, but in case we let the ArgumentNullException arise

        [JsonPropertyName("__capacitySkuType")]
        public CloudWorkspaceCapacitySkuType CapacitySkuType => (CloudWorkspaceCapacitySkuType)Enum.Parse(typeof(CloudWorkspaceCapacitySkuType), CapacitySku!); // here we don't expect null, but in case we let the ArgumentNullException arise
    }
}
