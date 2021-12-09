using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class FormatDaxResponse : List<FormatterResult>
    {
    }

    public class FormatterResult
    {
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
