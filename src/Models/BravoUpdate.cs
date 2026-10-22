using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models;

public class BravoUpdate
{
    [JsonPropertyName("isNewerVersion")]
    public bool IsNewerVersion { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("changelogUrl")]
    public string? ChangelogUrl { get; set; }
}
