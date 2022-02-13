namespace Sqlbi.Bravo.Models
{
    using AutoUpdaterDotNET;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using System.Text.Json.Serialization;

    public interface IUpdateInfo
    {
        UpdateChannelType? UpdateChannel { get; set; }

        string? CurrentVersion { get; set; }

        string? InstalledVersion { get; set; }

        string? DownloadUrl { get; set; }

        string? ChangelogUrl { get; set; }
    }

    public class BravoUpdate : IUpdateInfo
    {
        [JsonPropertyName("updateChannel")]
        public UpdateChannelType? UpdateChannel { get; set; }

        [JsonPropertyName("currentVersion")]
        public string? CurrentVersion { get; set; }

        [JsonPropertyName("installedVersion")]
        public string? InstalledVersion { get; set; }

        [JsonPropertyName("downloadUrl")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("changelogUrl")]
        public string? ChangelogUrl { get; set; }

        public static BravoUpdate CreateFrom(UpdateChannelType updateChannel, UpdateInfoEventArgs updateInfo)
        {
            var bravoUpdate = new BravoUpdate
            {
                UpdateChannel = updateChannel,
                DownloadUrl = updateInfo.DownloadURL,
                ChangelogUrl = updateInfo.ChangelogURL,
                CurrentVersion = updateInfo.CurrentVersion,
                InstalledVersion = updateInfo.InstalledVersion.ToString(),
            };

            return bravoUpdate;
        }
    }
}
