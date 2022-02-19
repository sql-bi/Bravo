namespace Sqlbi.Bravo.Models.AnalyzeModel
{
    using Sqlbi.Bravo.Models.FormatDax;
    using System.Diagnostics;
    using System.Text.Json.Serialization;

    [DebuggerDisplay("'{TableName}'[{Name}]")]
    public class TabularMeasure
    {
        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("tableName")]
        public string? TableName { get; set; }

        [JsonPropertyName("measure")]
        public string? Expression { get; set; }

        [JsonPropertyName("lineBreakStyle")]
        public DaxLineBreakStyle? LineBreakStyle { get; set; }
    }
}
