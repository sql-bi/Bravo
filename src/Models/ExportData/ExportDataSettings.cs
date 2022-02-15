namespace Sqlbi.Bravo.Models.ExportData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.IO;
    using System.Text.Json.Serialization;

    public abstract class ExportDataSettings
    {
        /// <summary>
        /// Names of tables to export
        /// </summary>
        [Required]
        [JsonPropertyName("tables")]
        public IEnumerable<string> Tables { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Full local path where the files will be created
        /// </summary>
        [JsonIgnore]
        public string ExportPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify), $"BravoExportData-{DateTime.Now:yyyyMMddHHmmss}");
    }

    public class ExportDelimitedTextSettings : ExportDataSettings
    {
        /// <summary>
        /// Specifies whether Unicode should be used as the character encoding for the file, otherwise UTF8 is used as default
        /// </summary>
        [JsonPropertyName("unicodeEncoding")]
        public bool UnicodeEncoding { get; set; } = false;

        /// <summary>
        /// Specifies the delimiter used to separate fields. If not provided <see cref="TextInfo.ListSeparator"/> is used as default
        /// </summary>
        [JsonPropertyName("delimiter")]
        public string? Delimiter { get; set; }

        /// <summary>
        /// Specifies if all string fields should be quoted. Default is false
        /// </summary>
        [JsonPropertyName("quoteStringFields")]
        public bool QuoteStringFields { get; set; } = false;
    }

    public class ExportExcelSettings : ExportDataSettings
    {
        /// <summary>
        /// Specifies whether an export summary worksheet should be created
        /// </summary>
        [JsonPropertyName("createExportSummary")]
        public bool CreateExportSummary { get; set; } = true;
    }
}
