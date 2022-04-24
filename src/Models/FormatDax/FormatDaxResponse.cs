namespace Sqlbi.Bravo.Models.FormatDax
{
    using Sqlbi.Bravo.Infrastructure;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class FormatDaxResponse : List<FormattedMeasure>
    {
        // TODO: do not inherit from Generic.List<T>, add instead a 'Measures' property 
        //[JsonPropertyName("measures")]
        //public IEnumerable<FormattedMeasure>? Measures { get; set; }
    }

    public class FormattedMeasure
    {
        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("tableName")]
        public string? TableName { get; set; }

        [JsonPropertyName("expression")]
        public string? Expression { get; set; }

        [JsonPropertyName("lineBreakStyle")]
        public DaxLineBreakStyle LineBreakStyle { get; set; } = AppEnvironment.FormatDaxLineBreakDefault;

        [JsonPropertyName("errors")]
        public IEnumerable<FormatterError>? Errors { get; set; }
    }

    public class FormatterError
    {
        [JsonPropertyName("line")]
        public int? Line { get; set; }

        [JsonPropertyName("column")]
        public int? Column { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
