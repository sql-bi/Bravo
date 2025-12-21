namespace Sqlbi.Bravo.Models.ManageCalendars
{
    /// <summary>
    /// Maps a column to a calendar category
    /// </summary>
    public class ColumnMapping
    {
        /// <summary>
        /// Name of the column being mapped
        /// </summary>
        public string? ColumnName { get; set; }

        /// <summary>
        /// The calendar category this column maps to (Year, Month, Quarter, TimeRelated, etc.)
        /// </summary>
        public CalendarColumnGroupType GroupType { get; set; }

        /// <summary>
        /// True if this is the primary column for this category (when multiple columns map to same category)
        /// Only applies to TimeUnit categories - TimeRelated columns have no primary/associated distinction
        /// </summary>
        public bool IsPrimary { get; set; } = true;

        /// <summary>
        /// True if this mapping comes from a "Sort By Column" relationship (read-only)
        /// These implicit associations cannot be edited directly
        /// </summary>
        public bool IsImplicitFromSortBy { get; set; } = false;

        /// <summary>
        /// If IsImplicitFromSortBy is true, this is the name of the column that references this column as its sort column
        /// </summary>
        public string? SortByParentColumn { get; set; }
    }
}
