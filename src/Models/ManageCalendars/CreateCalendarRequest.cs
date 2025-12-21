namespace Sqlbi.Bravo.Models.ManageCalendars
{
    /// <summary>
    /// Request to create a new calendar on a table
    /// </summary>
    public class CreateCalendarRequest
    {
        /// <summary>
        /// The Power BI Desktop report to update
        /// </summary>
        public PBIDesktopReport? Report { get; set; }

        /// <summary>
        /// Name of the table to add the calendar to
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// The calendar metadata to create
        /// </summary>
        public CalendarMetadata? Calendar { get; set; }
    }
}
