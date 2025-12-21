namespace Sqlbi.Bravo.Models.ManageCalendars
{
    /// <summary>
    /// Enum matching TOM TimeUnit enum for calendar categories
    /// Maps to Microsoft.AnalysisServices.Tabular.TimeUnit
    /// </summary>
    public enum CalendarColumnGroupType
    {
        /// <summary>
        /// Explicitly marked as unassigned by the user
        /// </summary>
        Unassigned = -1,

        /// <summary>
        /// TimeUnit.Unknown (0) - Unknown time unit
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// TimeUnit.Year (1) - Year level
        /// </summary>
        Year = 1,

        /// <summary>
        /// TimeUnit.Semester (2) - Semester level
        /// </summary>
        Semester = 2,

        /// <summary>
        /// TimeUnit.SemesterOfYear (3) - Semester of year
        /// </summary>
        SemesterOfYear = 3,

        /// <summary>
        /// TimeUnit.Quarter (4) - Quarter level
        /// </summary>
        Quarter = 4,

        /// <summary>
        /// TimeUnit.QuarterOfYear (5) - Quarter of year
        /// </summary>
        QuarterOfYear = 5,

        /// <summary>
        /// TimeUnit.QuarterOfSemester (6) - Quarter of semester
        /// </summary>
        QuarterOfSemester = 6,

        /// <summary>
        /// TimeUnit.Month (7) - Month level
        /// </summary>
        Month = 7,

        /// <summary>
        /// TimeUnit.MonthOfYear (8) - Month of year
        /// </summary>
        MonthOfYear = 8,

        /// <summary>
        /// TimeUnit.MonthOfSemester (9) - Month of semester
        /// </summary>
        MonthOfSemester = 9,

        /// <summary>
        /// TimeUnit.MonthOfQuarter (10) - Month of quarter
        /// </summary>
        MonthOfQuarter = 10,

        /// <summary>
        /// TimeUnit.Week (11) - Week level
        /// </summary>
        Week = 11,

        /// <summary>
        /// TimeUnit.WeekOfYear (12) - Week of year
        /// </summary>
        WeekOfYear = 12,

        /// <summary>
        /// TimeUnit.WeekOfSemester (13) - Week of semester
        /// </summary>
        WeekOfSemester = 13,

        /// <summary>
        /// TimeUnit.WeekOfQuarter (14) - Week of quarter
        /// </summary>
        WeekOfQuarter = 14,

        /// <summary>
        /// TimeUnit.WeekOfMonth (15) - Week of month
        /// </summary>
        WeekOfMonth = 15,

        /// <summary>
        /// TimeUnit.Date (16) - Date/day level
        /// </summary>
        Date = 16,

        /// <summary>
        /// TimeUnit.DayOfYear (17) - Day of year
        /// </summary>
        DayOfYear = 17,

        /// <summary>
        /// TimeUnit.DayOfSemester (18) - Day of semester
        /// </summary>
        DayOfSemester = 18,

        /// <summary>
        /// TimeUnit.DayOfQuarter (19) - Day of quarter
        /// </summary>
        DayOfQuarter = 19,

        /// <summary>
        /// TimeUnit.DayOfMonth (20) - Day of month
        /// </summary>
        DayOfMonth = 20,

        /// <summary>
        /// TimeUnit.DayOfWeek (21) - Day of week
        /// </summary>
        DayOfWeek = 21,

        /// <summary>
        /// TimeRelatedColumnGroup - Columns with filter context removed by DAX time intelligence functions
        /// These columns are considered part of the Calendar and have their filters removed by time intelligence functions
        /// </summary>
        TimeRelated = 100
    }
}
