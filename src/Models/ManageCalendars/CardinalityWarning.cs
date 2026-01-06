namespace Sqlbi.Bravo.Models.ManageCalendars
{
    /// <summary>
    /// Represents a cardinality mismatch warning for a column in a calendar
    /// </summary>
    public class CardinalityWarning
    {
        /// <summary>
        /// Name of the calendar where the warning occurs
        /// </summary>
        public string? CalendarName { get; set; }

        /// <summary>
        /// Name of the column with the cardinality mismatch
        /// </summary>
        public string? ColumnName { get; set; }

        /// <summary>
        /// The category assigned to the column
        /// </summary>
        public CalendarColumnGroupType Category { get; set; }

        /// <summary>
        /// Actual cardinality from the column's # VALUES
        /// </summary>
        public long ActualCardinality { get; set; }

        /// <summary>
        /// Expected cardinality (or minimum for ranges)
        /// </summary>
        public long ExpectedMin { get; set; }

        /// <summary>
        /// Expected maximum cardinality (for ranges), null for exact values
        /// </summary>
        public long? ExpectedMax { get; set; }

        /// <summary>
        /// Human-readable expectation description (e.g., "12", "52-53", "12 × Year")
        /// </summary>
        public string? ExpectedDescription { get; set; }
    }
}
