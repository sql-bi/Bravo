using System;
using System.Text.Json.Serialization;

#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    public class SharedDatasetModel
    {
        private const int PBIXProviderId = 7;

        private const string ExcelModelResource = "ExcelModelResource";

        public long Id { get; set; }

        public string DisplayName { get; set; }

        public string? Description { get; set; }

        public SharedDatasetPermissions Permissions { get; set; }

        public string DBName { get; set; }

        public string VSName { get; set; }

        public bool? CloudRLSEnabled { get; set; }

        public bool DirectQueryMode { get; set; }

        [JsonPropertyName("LastRefreshTime")]
        public DateTime LastRefreshTime { get; set; }

        //Ignored property - this is the old microsoft datetimeoffset jsonserializer format e.g. /Date(1617810742483)/
        //[JsonPropertyName("lastRefreshTime")]
        //public string lastRefreshTime { get; set; }

        public SharedDatasetUser CreatorUser { get; set; }

        public bool InsightsSupported { get; set; }

        public string ResourceName { get; set; }

        public DateTime NextRefreshTime { get; set; }

        public string? OnPremModelConnectionString { get; set; }

        public int? PushDataVersion { get; set; }

        public int? RealTimeMode { get; set; }

        public int? ContentProviderId { get; set; }

        public long? OriginalModelId { get; set; }

        public bool? IsHidden { get; set; }

        public bool IsCloudModel => !DirectQueryMode && !IsPushDataEnabled && string.IsNullOrEmpty(OnPremModelConnectionString);

        public bool IsOnPremModel => !string.IsNullOrEmpty(OnPremModelConnectionString);

        public bool IsExcelWorkbook => ExcelModelResource.Equals(ResourceName, StringComparison.OrdinalIgnoreCase);

        public bool IsWritablePbixModel => IsWriteableModel && ContentProviderId == PBIXProviderId;

        public bool IsWriteableModel => Permissions.HasFlag(SharedDatasetPermissions.Write);

        public bool IsPushDataEnabled => PushDataVersion != 0;

        public bool IsPushStreaming => PushDataVersion != 0 && RealTimeMode != 0;
    }
}
