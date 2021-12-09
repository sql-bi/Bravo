using Dax.Formatter.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class FormatDaxRequest
    {
        [JsonPropertyName("options")]
        public FormatDaxOptions? Options { get; set; }

        [JsonPropertyName("measures")]
        public IEnumerable<TabularMeasure>? Measures { get; set; }
    }

    public class FormatDaxOptions
    {
        [JsonPropertyName("lineStyle")]
        public DaxFormatterLineStyle? LineStyle { get; set; }

        [JsonPropertyName("spacingStyle")]
        public DaxFormatterSpacingStyle? SpacingStyle { get; set; }

        //[JsonPropertyName("separatorStyle")]
        //public DaxFormatterSeparatorStyle SeparatorStyle { get; set; }

        [JsonPropertyName("listSeparator")]
        public char? ListSeparator { get; set; }

        [JsonPropertyName("decimalSeparator")]
        public char? DecimalSeparator { get; set; }

 /*
        TODO: available Dax.Formatter options

        string ServerName
        ServerEdition? ServerEdition
        ServerType? ServerType
        ServerMode? ServerMode
        ServerLocation? ServerLocation
        string ServerVersion
        string DatabaseName
        string DatabaseCompatibilityLevel
 */
    }
}
