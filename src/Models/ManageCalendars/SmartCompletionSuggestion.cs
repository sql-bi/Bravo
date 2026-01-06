namespace Sqlbi.Bravo.Models.ManageCalendars
{
    /// <summary>
    /// Represents a suggested category assignment from smart completion
    /// </summary>
    public class SmartCompletionSuggestion
    {
        /// <summary>
        /// Name of the calendar for this suggestion
        /// </summary>
        public string? CalendarName { get; set; }

        /// <summary>
        /// Name of the column to assign
        /// </summary>
        public string? ColumnName { get; set; }

        /// <summary>
        /// Suggested category based on cardinality matching
        /// </summary>
        public CalendarColumnGroupType SuggestedCategory { get; set; }

        /// <summary>
        /// Whether this column should be assigned as primary (true) or associated (false)
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// The cardinality that led to this suggestion
        /// </summary>
        public long ColumnCardinality { get; set; }

        /// <summary>
        /// The Year cardinality used for calculation
        /// </summary>
        public long YearCardinality { get; set; }
    }
}
