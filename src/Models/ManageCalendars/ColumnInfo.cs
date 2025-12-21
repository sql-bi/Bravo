namespace Sqlbi.Bravo.Models.ManageCalendars
{
    using System.Collections.Generic;

    /// <summary>
    /// Information about a single column in the table
    /// </summary>
    public class ColumnInfo
    {
        /// <summary>
        /// Name of the column
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Data type of the column (e.g., "String", "Int64", "DateTime")
        /// </summary>
        public string? DataType { get; set; }

        /// <summary>
        /// Sample values from the column (limited to a few rows)
        /// </summary>
        public List<object>? SampleValues { get; set; }
    }
}
