namespace Sqlbi.Bravo.Models.FormatDax
{
    using Dax.Formatter.Models;
    using Sqlbi.Bravo.Infrastructure;
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
        /// <summary>
        /// Auto-calculated <see cref="DaxLineBreakStyle"/> based on the existing measures in the model. See <see cref="TabularDatabaseInfo.AutoLineBreakStyle"/>
        /// </summary>
        [Required]
        [JsonPropertyName("autoLineBreakStyle")]
        public DaxLineBreakStyle? AutoLineBreakStyle { get; set; }

        /// <summary>
        /// Preferred <see cref="DaxLineBreakStyle"/> from user settings
        /// </summary>
        [JsonPropertyName("lineBreakStyle")]
        public DaxLineBreakStyle LineBreakStyle { get; set; } = AppEnvironment.FormatDaxLineBreakDefault;

        [JsonPropertyName("lineStyle")]
        public DaxFormatterLineStyle? LineStyle { get; set; }

        [JsonPropertyName("spacingStyle")]
        public DaxFormatterSpacingStyle? SpacingStyle { get; set; }

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

    public enum DaxLineBreakStyle
    {
        /// <summary>
        /// No line break character at beginning of DAX expression
        /// </summary>
        None = 0,

        /// <summary>
        /// The first character of the DAX expression is a line break
        /// </summary>
        InitialLineBreak = 1,

        ///// <summary>
        ///// Like <see cref="InitialLineBreak"/> only for multi-line DAX expressions
        ///// </summary>
        //InitiaLineBreakOnMultilineOnly = 2,

        /// <summary>
        /// Automatically pick <see cref="None"/> or <see cref="InitialLineBreak"/> based on the existing measures in the model, using the prevalent technique in existing measures
        /// </summary>
        Auto = 3
    }
}
