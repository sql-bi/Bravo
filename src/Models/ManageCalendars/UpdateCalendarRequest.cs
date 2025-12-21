namespace Sqlbi.Bravo.Models.ManageCalendars
{
    /// <summary>
    /// Request to update an existing calendar
    /// </summary>
    public class UpdateCalendarRequest
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
        /// Name of the calendar to update
        /// </summary>
        public string? CalendarName { get; set; }

        /// <summary>
        /// The updated calendar metadata
        /// </summary>
        public CalendarMetadata? Calendar { get; set; }
    }
}
