namespace Sqlbi.Bravo.Models
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using System.Text.Json.Serialization;
    using System.Xml.Linq;

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

        public static BravoUpdate CreateFrom(UpdateChannelType updateChannel, XDocument document)
        {
            var bravoUpdate = new BravoUpdate
            {
                UpdateChannel = updateChannel,
                InstalledVersion = AppEnvironment.ApplicationFileVersion,
            };

            if (document.Root is not null)
            {
                bravoUpdate.DownloadUrl = document.Root.Element("url")?.Value;
                bravoUpdate.ChangelogUrl = document.Root.Element("changelog")?.Value;
                bravoUpdate.CurrentVersion = document.Root.Element("version")?.Value;
            }

            return bravoUpdate;
        }
    }
}
