namespace Sqlbi.Bravo.Models.ManageDates
{
    using Dax.Template.Tables;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System;
    using System.Globalization;
    using System.Text.Json.Serialization;

    public class DateDefaults
    {
        [JsonPropertyName("firstFiscalMonth")]
        public int? FirstFiscalMonth { get; set; }

        [JsonPropertyName("firstDayOfWeek")]
        public DayOfWeek? FirstDayOfWeek { get; set; }

        [JsonPropertyName("monthsInYear")]
        public int? MonthsInYear { get; set; }

        [JsonPropertyName("workingDayType")]
        public string? WorkingDayType { get; set; }

        [JsonPropertyName("nonWorkingDayType")]
        public string? NonWorkingDayType { get; set; }

        [JsonPropertyName("typeStartFiscalYear")]
        public TypeStartFiscalYear? TypeStartFiscalYear { get; set; }

        [JsonPropertyName("quarterWeekType")]
        public QuarterWeekType? QuarterWeekType { get; set; }

        [JsonPropertyName("weeklyType")]
        public WeeklyType? WeeklyType { get; set; }

        public void CopyTo(TemplateConfiguration templateConfiguration)
        {
            Set(nameof(FirstFiscalMonth), FirstFiscalMonth, quoted: false);
            Set(nameof(FirstDayOfWeek), (int?)FirstDayOfWeek, quoted: false);
            Set(nameof(MonthsInYear), MonthsInYear, quoted: false);
            Set(nameof(TypeStartFiscalYear), (int?)TypeStartFiscalYear, quoted: false);
            Set(nameof(WorkingDayType), WorkingDayType, quoted: true);
            Set(nameof(NonWorkingDayType), NonWorkingDayType, quoted: true);
            Set(nameof(QuarterWeekType), (int?)QuarterWeekType, quoted: true);
            Set(nameof(WeeklyType), WeeklyType, quoted: true);

            void Set<T>(string name, T? value, bool quoted)
            {
                if (value is null)
                    return;

                var parameterKey = $"__{ name }";

                if (templateConfiguration.DefaultVariables.ContainsKey(parameterKey))
                {
                    var parameterValue = quoted ? $"\"{ value }\"" : $"{ value }";

                    templateConfiguration.DefaultVariables[parameterKey] = parameterValue;
                }
            }
        }

        public static DateDefaults CreateFrom(TemplateConfiguration templateConfiguration)
        {
            DateDefaults dateDefaults = new();

            dateDefaults.FirstFiscalMonth = GetInt(nameof(FirstFiscalMonth));
            dateDefaults.FirstDayOfWeek = GetString(nameof(FirstDayOfWeek)).TryParseTo<DayOfWeek>();
            dateDefaults.MonthsInYear = GetInt(nameof(MonthsInYear));
            dateDefaults.WorkingDayType = GetString(nameof(WorkingDayType), unquote: true);
            dateDefaults.NonWorkingDayType = GetString(nameof(NonWorkingDayType), unquote: true);
            dateDefaults.TypeStartFiscalYear = GetString(nameof(TypeStartFiscalYear)).TryParseTo<TypeStartFiscalYear>();
            dateDefaults.QuarterWeekType = GetString(nameof(QuarterWeekType), unquote: true).TryParseTo<QuarterWeekType>();
            dateDefaults.WeeklyType = GetString(nameof(WeeklyType), unquote: true).TryParseTo<WeeklyType>();

            // Override the template value only if the variable exists, otherwise keep the null value
            if (dateDefaults.FirstFiscalMonth is not null)
                dateDefaults.FirstFiscalMonth = 0; // Zero-based

            // Override the template value only if the variable exists, otherwise keep the null value
            if (dateDefaults.FirstDayOfWeek is not null)
                dateDefaults.FirstDayOfWeek = DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek;

            return dateDefaults;

            int? GetInt(string? name)
            {
                var value = GetString(name);
                if (value == null)
                    return null;

                if (int.TryParse(value, out var valueInt))
                    return valueInt;

                return null;
            }

            string? GetString(string? name, bool unquote = false)
            {
                if (name.IsNullOrEmpty())
                    return null;

                var parameterKey = $"__{ name }";

                if (templateConfiguration.DefaultVariables.TryGetValue(parameterKey, out var parameterValue))
                {
                    if (unquote)
                    {
                        if (parameterValue[0] == '"' && parameterValue[^1] == '"')
                            parameterValue = parameterValue[1..^1];
                    }

                    return parameterValue;
                }

                return null;
            }
        }
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
