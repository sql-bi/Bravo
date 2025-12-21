namespace Sqlbi.Bravo.Models.ManageCalendars
{
    /// <summary>
    /// Request to delete a calendar from a table
    /// </summary>
    public class DeleteCalendarRequest
    {
        /// <summary>
        /// The Power BI Desktop report to update
        /// </summary>
        public PBIDesktopReport? Report { get; set; }

        /// <summary>
        /// Name of the table containing the calendar
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// Name of the calendar to delete
        /// </summary>
        public string? CalendarName { get; set; }
    }
}
