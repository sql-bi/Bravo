namespace Sqlbi.Bravo.Models.ManageCalendars
{
    using System.Collections.Generic;

    /// <summary>
    /// Contains all calendar information for a specific table
    /// </summary>
    public class TableCalendarInfo
    {
        /// <summary>
        /// Name of the table (e.g., "Date")
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// List of all columns in the table with their sample data
        /// </summary>
        public List<ColumnInfo>? Columns { get; set; }

        /// <summary>
        /// List of all calendars defined on this table
        /// </summary>
        public List<CalendarMetadata>? Calendars { get; set; }
    }
}
