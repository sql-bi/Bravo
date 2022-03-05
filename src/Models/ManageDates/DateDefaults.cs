namespace Sqlbi.Bravo.Models.ManageDates
{
    using Dax.Template.Tables;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System;
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
            SetIntVariable(nameof(FirstFiscalMonth), FirstFiscalMonth);
            SetIntVariable(nameof(FirstDayOfWeek), (int?)FirstDayOfWeek);
            SetIntVariable(nameof(MonthsInYear), MonthsInYear);
            SetStringVariable(nameof(WorkingDayType), WorkingDayType);
            SetStringVariable(nameof(NonWorkingDayType), NonWorkingDayType);
            SetIntVariable(nameof(TypeStartFiscalYear), (int?)TypeStartFiscalYear);
            SetStringVariable(nameof(QuarterWeekType), (int?)QuarterWeekType);
            SetStringVariable(nameof(WeeklyType), WeeklyType);

            void SetStringVariable<T>(string parameterName, T? value)
            {
                SetVariable(parameterName, value, "\"");
            }

            void SetIntVariable<T>(string parameterName, T? value)
            {
                SetVariable(parameterName, value, "");
            }

            void SetVariable<T>(string parameterName, T? value, string quote)
            {
                if (value is null)
                    return;

                var variableKey = $"__{parameterName}";

                BravoUnexpectedException.Assert(templateConfiguration.DefaultVariables.ContainsKey(variableKey));

                var variableValue = value.ToString();

                BravoUnexpectedException.ThrowIfNull(variableValue);

                templateConfiguration.DefaultVariables[variableKey] = $"{ quote }{ variableValue }{ quote }";
            }
        }

        public static DateDefaults CreateFrom(TemplateConfiguration templateConfiguration)
        {
            var defaults = new DateDefaults
            {
                FirstFiscalMonth = GetIntParameter(nameof(FirstFiscalMonth)),
                FirstDayOfWeek = (DayOfWeek?)GetIntParameter(nameof(FirstDayOfWeek)),
                MonthsInYear = GetIntParameter(nameof(MonthsInYear)),
                WorkingDayType = GetQuotedStringParameter(nameof(WorkingDayType)),
                NonWorkingDayType = GetQuotedStringParameter(nameof(NonWorkingDayType)),
                TypeStartFiscalYear = (TypeStartFiscalYear?)GetIntParameter(nameof(TypeStartFiscalYear)),
            };

            var quarterWeekTypeParameter = GetQuotedStringParameter(nameof(QuarterWeekType));

            if (Enum.TryParse(quarterWeekTypeParameter, out QuarterWeekType quarterWeekType)) 
                defaults.QuarterWeekType = quarterWeekType;

            var weeklyTypeParameter = GetQuotedStringParameter(nameof(WeeklyType));

            if (Enum.TryParse(weeklyTypeParameter, out WeeklyType weeklyType)) 
                defaults.WeeklyType = weeklyType;

            return defaults;

            int? GetIntParameter(string? parameterName)
            {
                var value = GetStringParameter(parameterName);
                if (value == null)
                    return null;

                if (int.TryParse(value, out var valueInt))
                    return valueInt;

                return null;
            }

            string? GetStringParameter(string? parameterName)
            {
                if (parameterName.IsNullOrEmpty())
                    return null;

                if (templateConfiguration.DefaultVariables.TryGetValue($"__{ parameterName }", out var parameterValue))
                    return parameterValue;

                return null;
            }

            string? GetQuotedStringParameter(string? parameterName)
            {
                var parameterValue = GetStringParameter(parameterName);
                if (parameterValue.IsNullOrEmpty())
                    return null;

                if ((parameterValue[0] == '"') && (parameterValue[^1] == '"'))
                    parameterValue = parameterValue[1..^1];

                return parameterValue;
            }
        }
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
