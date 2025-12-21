namespace Sqlbi.Bravo.Models.ManageCalendars
{
    /// <summary>
    /// Request to get all calendars for a specific table
    /// </summary>
    public class GetTableCalendarsRequest
    {
        /// <summary>
        /// The Power BI Desktop report to query
        /// </summary>
        public PBIDesktopReport? Report { get; set; }

        /// <summary>
        /// Name of the table to get calendars for (defaults to "Date")
        /// </summary>
        public string? TableName { get; set; } = "Date";
    }
}
