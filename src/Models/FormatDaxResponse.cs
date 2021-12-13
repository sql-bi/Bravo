using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class FormatDaxResponse : List<FormattedMeasure>
    {
    }

    public class FormattedMeasure
    {
        [JsonPropertyName("etag")]
        public long? ETag { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("tableName")]
        public string? TableName { get; set; }

        [JsonPropertyName("measure")]
        public string? Expression { get; set; }

        [JsonPropertyName("errors")]
        public IEnumerable<FormatterError>? Errors { get; set; }
    }

    public class FormatterError
    {
        [JsonPropertyName("line")]
        public int Line { get; set; }

        [JsonPropertyName("column")]
        public int Column { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
