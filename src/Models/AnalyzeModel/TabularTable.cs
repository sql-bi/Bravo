namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using System.Diagnostics;
    using System.Text.Json.Serialization;

    [DebuggerDisplay("{Name}")]
    public class TabularTable
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("rowsCount")]
        public long RowsCount { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
