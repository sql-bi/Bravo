namespace Sqlbi.Bravo.Models.ManageDates
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class DateBravo
    {
        [JsonPropertyName("referencedTables")]
        public DateReferencedTable[]? ReferencedTables { get; set; }
    }

    public class DateReferencedTable
    {
        [Required]
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("action")]
        public ReferencedTableAction? Action { get; set; } = ReferencedTableAction.Unknown;

        [Required]
        [JsonPropertyName("paths")]
        public string[]? Paths { get; set; }
    }

    public enum ReferencedTableAction
    {
        Unknown = 0,

        /// <summary>
        /// A table with the same name does not exist and will be created
        /// </summary>
        ValidCreateNew = 1,

        /// <summary>
        /// A table with the same name exists and can be replaced
        /// </summary>
        ValidOverwrite = 2,

        /// <summary>
        /// A table with the same name exists and cannot be replaced, a different name is required
        /// </summary>
        InvalidRenameRequired = 100,
    }
}
