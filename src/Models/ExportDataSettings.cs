using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public abstract class ExportDataSettings
    {
        /// <summary>
        /// Names of tables to export
        /// </summary>
        [Required]
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
        public bool UnicodeEncoding { get; set; } = false;

        /// <summary>
        /// Specifies the delimiter used to separate fields. If not provided <see cref="System.Globalization.TextInfo.ListSeparator"/> is used as default
        /// </summary>
        public string? Delimiter { get; set; }

        /// <summary>
        /// Specifies if all string fields should be quoted. Default is false
        /// </summary>
        public bool QuoteStringFields { get; set; } = false;
    }

    public class ExportExcelSettings : ExportDataSettings
    {
    }
}
