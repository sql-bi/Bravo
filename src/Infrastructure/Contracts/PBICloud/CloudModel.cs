namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System;
    using System.Text.Json.Serialization;

    public sealed class CloudModel
    {
        private const int PBIXProviderId = 7;

        private const string ExcelModelResource = "ExcelModelResource";

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("dbName")]
        public string? DBName { get; set; }

        [JsonPropertyName("vsName")]
        public string? VSName { get; set; }

        [JsonPropertyName("permissions")]
        public CloudPermissions Permissions { get; set; }

        [JsonPropertyName("resourceName")]
        public string? ResourceName { get; set; }

        [JsonPropertyName("nextRefreshTime")]
        public DateTime NextRefreshTime { get; set; }

        [JsonPropertyName("LastRefreshTime")]
        public DateTime LastRefreshTime { get; set; }

        //Ignored property - this is the old Microsoft datetimeoffset jsonserializer format e.g. /Date(1617810742483)/
        //[JsonPropertyName("lastRefreshTime")]
        //public string lastRefreshTime { get; set; }

        [JsonPropertyName("creatorUser")]
        public CloudUser? CreatorUser { get; set; }

        [JsonPropertyName("insightsSupported")]
        public bool InsightsSupported { get; set; }

        public bool CloudRLSEnabled { get; set; }

        public string? OnPremModelConnectionString { get; set; }

        public bool DirectQueryMode { get; set; }

        public int PushDataVersion { get; set; }

        public int RealTimeMode { get; set; }

        public int ContentProviderId { get; set; }

        public long? OriginalModelId { get; set; }

        public bool IsHidden { get; set; }

        public bool IsCloudModel => !DirectQueryMode && !IsPushDataEnabled && string.IsNullOrEmpty(OnPremModelConnectionString);

        public bool IsOnPremModel => !string.IsNullOrEmpty(OnPremModelConnectionString);

        public bool IsExcelWorkbook => ExcelModelResource.Equals(ResourceName, StringComparison.OrdinalIgnoreCase);

        public bool IsWritablePbixModel => IsWriteableModel && ContentProviderId == PBIXProviderId;

        public bool IsWriteableModel => Permissions.HasFlag(CloudPermissions.Write);

        public bool IsPushDataEnabled => PushDataVersion != 0;

        public bool IsPushStreaming => IsPushDataEnabled && RealTimeMode != 0;
    }
}
