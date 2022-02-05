namespace Sqlbi.Bravo.Models.ManageDates
{
    using System.Text.Json.Serialization;

    public class DateDefaults
    {
        [JsonPropertyName("firstFiscalMonth")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? FirstFiscalMonth { get; set; }

        [JsonPropertyName("firstDayOfWeek")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DayOfWeek? FirstDayOfWeek { get; set; }

        [JsonPropertyName("monthsInYear")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MonthsInYear { get; set; }

        [JsonPropertyName("workingDayType")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? WorkingDayType { get; set; }

        [JsonPropertyName("nonWorkingDayType")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NonWorkingDayType { get; set; }

        [JsonPropertyName("typeStartFiscalYear")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TypeStartFiscalYear? TypeStartFiscalYear { get; set; }

        [JsonPropertyName("quarterWeekType")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public QuarterWeekType? QuarterWeekType { get; set; }

        [JsonPropertyName("weeklyType")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public WeeklyType? WeeklyType { get; set; }
    }

    public enum DayOfWeek
    {
        Sunday = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6,
    }

    public enum TypeStartFiscalYear
    {
        FirstDayOfFiscalYear = 0,
        LastDayOfFiscalYear = 1,
    }

    public enum WeeklyType
    {
        Last = 0,
        Nearest = 1,
    }

    public enum QuarterWeekType
    {
        Weekly445 = 445,
        Weekly454 = 454,
        Weekly544 = 544,
    }
}
