using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models;

public interface IUpdateInfo
{
    bool IsNewerVersion { get; set; }

    string? Version { get; set; }

    string? DownloadUrl { get; set; }

    string? ChangelogUrl { get; set; }
}

public class BravoUpdate : IUpdateInfo
{
    [JsonPropertyName("isNewerVersion")]
    public bool IsNewerVersion { get; set; } = false;

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("changelogUrl")]
    public string? ChangelogUrl { get; set; }
}
