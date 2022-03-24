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

        //Ignored property - this is the old .NET JavaScriptSerializer/DataContractJsonSerializer date format e.g. /Date(1617810742483)/
        //[JsonPropertyName("lastRefreshTime")]
        //public string lastRefreshTime { get; set; }

        [JsonPropertyName("creatorUser")]
        public CloudUser? CreatorUser { get; set; }

        [JsonPropertyName("insightsSupported")]
        public bool InsightsSupported { get; set; }

        [JsonPropertyName("cloudRlsEnabled")]
        public bool CloudRLSEnabled { get; set; }

        [JsonPropertyName("onPremModelConnectionString")]
        public string? OnPremModelConnectionString { get; set; }

        [JsonPropertyName("directQueryMode")]
        public bool DirectQueryMode { get; set; }

        [JsonPropertyName("pushDataVersion")]
        public int PushDataVersion { get; set; }

        [JsonPropertyName("realTimeMode")]
        public int RealTimeMode { get; set; }

        [JsonPropertyName("contentProviderId")]
        public int ContentProviderId { get; set; }

        [JsonPropertyName("originalModelId")]
        public long? OriginalModelId { get; set; }

        [JsonPropertyName("isHidden")]
        public bool IsHidden { get; set; }

        [JsonPropertyName("__isCloudModel")]
        public bool IsCloudModel => !DirectQueryMode && !IsPushDataEnabled && string.IsNullOrEmpty(OnPremModelConnectionString);

        [JsonPropertyName("__isOnPremModel")]
        public bool IsOnPremModel => !string.IsNullOrEmpty(OnPremModelConnectionString);

        [JsonPropertyName("__isExcelWorkbook")]
        public bool IsExcelWorkbook => ExcelModelResource.Equals(ResourceName, StringComparison.OrdinalIgnoreCase);

        [JsonPropertyName("__isWritablePbixModel")]
        public bool IsWritablePbixModel => IsWriteableModel && ContentProviderId == PBIXProviderId;

        [JsonPropertyName("__isWriteableModel")]
        public bool IsWriteableModel => Permissions.HasFlag(CloudPermissions.Write);

        [JsonPropertyName("__isPushDataEnabled")]
        public bool IsPushDataEnabled => PushDataVersion != 0;

        [JsonPropertyName("__isPushStreaming")]
        public bool IsPushStreaming => IsPushDataEnabled && RealTimeMode != 0;
    }
}
