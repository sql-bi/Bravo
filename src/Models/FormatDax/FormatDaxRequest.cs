namespace Sqlbi.Bravo.Models.FormatDax
{
    using Dax.Formatter.Models;
    using Sqlbi.Bravo.Models.AnalyzeModel;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class FormatDaxRequest
    {
        [Required]
        [JsonPropertyName("options")]
        public FormatDaxOptions? Options { get; set; }

        [Required]
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
