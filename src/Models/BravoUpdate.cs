namespace Sqlbi.Bravo.Models
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using System.Text.Json.Serialization;

    public interface IUpdateInfo
    {
        UpdateChannelType? UpdateChannel { get; set; }

        bool IsNewerVersion { get; set; }

        string? Version { get; set; }

        string? DownloadUrl { get; set; }

        string? ChangelogUrl { get; set; }
    }

    public class BravoUpdate : IUpdateInfo
    {
        [JsonPropertyName("updateChannel")]
        public UpdateChannelType? UpdateChannel { get; set; }

        [JsonPropertyName("isNewerVersion")]
        public bool IsNewerVersion { get; set; } = false;

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("downloadUrl")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("changelogUrl")]
        public string? ChangelogUrl { get; set; }
    }
}
