namespace Sqlbi.Bravo.Models
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using System.Text.Json.Serialization;

    public interface IUpdateInfo
    {
        UpdateChannelType? UpdateChannel { get; set; }

        bool IsNewerVersion { get; set; }

        string? CurrentVersion { get; set; }

        string? InstalledVersion { get; set; }

        string? DownloadUrl { get; set; }

        string? ChangelogUrl { get; set; }
    }

    public class BravoUpdate : IUpdateInfo
    {
        [JsonPropertyName("updateChannel")]
        public UpdateChannelType? UpdateChannel { get; set; }

        [JsonPropertyName("isNewerVersion")]
        public bool IsNewerVersion { get; set; } = false;

        [JsonPropertyName("currentVersion")]
        public string? CurrentVersion { get; set; }

        [JsonPropertyName("installedVersion")]
        public string? InstalledVersion { get; set; }

        [JsonPropertyName("downloadUrl")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("changelogUrl")]
        public string? ChangelogUrl { get; set; }
    }
}
