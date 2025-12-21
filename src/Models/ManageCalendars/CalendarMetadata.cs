namespace Sqlbi.Bravo.Models.ManageCalendars
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a Calendar and its column mappings for serialization
    /// </summary>
    public class CalendarMetadata
    {
        /// <summary>
        /// Name of the calendar (e.g., "Gregorian", "Fiscal 445")
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Description of the calendar
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Collection of column mappings for this calendar
        /// </summary>
        public List<ColumnMapping>? ColumnMappings { get; set; }
    }
}
