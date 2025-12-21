namespace Sqlbi.Bravo.Models.ManageCalendars
{
    using System.Collections.Generic;

    /// <summary>
    /// Stores Bravo-specific annotations for calendar management
    /// Persisted in TOM extended properties
    /// </summary>
    public class CalendarAnnotations
    {
        /// <summary>
        /// Columns explicitly marked as "Unassigned" by the user
        /// Key: CalendarName, Value: List of column names
        /// This distinguishes between:
        /// - Blank: New column, no decision made yet
        /// - Unassigned: User explicitly decided this column doesn't belong to any category
        /// </summary>
        public Dictionary<string, List<string>>? ExplicitlyUnassignedColumns { get; set; }

        /// <summary>
        /// Property name for storing in TOM extended properties
        /// </summary>
        public const string AnnotationPropertyName = "SQLBI_BRAVO_CalendarAnnotations";
    }
}
