namespace Sqlbi.Bravo.Models.ManageDates
{
    using Dax.Template.Enums;
    using Dax.Template.Tables;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System;
    using System.Text.Json.Serialization;

    public class DateConfiguration
    {
        /// <summary>
        /// This property is for internal use, it must not be shown in Bravo UI
        /// </summary>
        public string TemplatePath { get; init; }

        public string? Name { get; init; }

        public string? Description { get; init; }

        public string? IsoFormat { get; set; }

        public string? IsoCountry { get; set; }

        public string? IsoTranslation { get; set; }

        //*

        public string[]? OnlyTablesColumns { get; set; }

        public string[]? ExceptTablesColumns { get; set; }

        public int? FirstYear { get; set; }

        public int? LastYear { get; set; }

        public string? TableSingleInstanceMeasures { get; set; }

        public string[]? TargetMeasures { get; set; }

        //*

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AutoScanEnum? AutoScan { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AutoNamingEnum? AutoNaming { get; set; }

        public DateDefaults Defaults { get; init; } = new();

        public static DateConfiguration CreateFrom(TemplateConfiguration templateConfiguration)
        {
            var configuration = new DateConfiguration
            {
                // TemplatePath = package.Path *** TODO ***

                Name = templateConfiguration.Name,
                Description = templateConfiguration.Description,
                IsoFormat = templateConfiguration.IsoFormat,
                IsoCountry = templateConfiguration.IsoCountry,
                IsoTranslation = templateConfiguration.IsoTranslation,

                // ???? OnlyTablesColumns
                // ???? ExceptTablesColumns
                // ???? FirstYear
                // ???? LastYear
                // ???? TableSingleInstanceMeasures
                // ???? TargetMeasures

                AutoScan = templateConfiguration.AutoScan,
                AutoNaming = templateConfiguration.AutoNaming
            };

            configuration.Defaults.FirstFiscalMonth = GetIntParameter(nameof(configuration.Defaults.FirstFiscalMonth));
            configuration.Defaults.FirstDayOfWeek = (DayOfWeek?)GetIntParameter(nameof(configuration.Defaults.FirstDayOfWeek));
            configuration.Defaults.MonthsInYear = GetIntParameter(nameof(configuration.Defaults.MonthsInYear));
            configuration.Defaults.WorkingDayType = GetQuotedStringParameter(nameof(configuration.Defaults.WorkingDayType));
            configuration.Defaults.NonWorkingDayType = GetQuotedStringParameter(nameof(configuration.Defaults.NonWorkingDayType));
            configuration.Defaults.TypeStartFiscalYear = (TypeStartFiscalYear?)GetIntParameter(nameof(configuration.Defaults.TypeStartFiscalYear));

            var quarterWeekTypeParameter = GetQuotedStringParameter(nameof(configuration.Defaults.QuarterWeekType));

            if (Enum.TryParse(quarterWeekTypeParameter, out QuarterWeekType quarterWeekType))
            {
                configuration.Defaults.QuarterWeekType = quarterWeekType;
            }

            var weeklyTypeParameter = GetQuotedStringParameter(nameof(configuration.Defaults.WeeklyType));

            if (Enum.TryParse(weeklyTypeParameter, out WeeklyType weeklyType))
            {
                configuration.Defaults.WeeklyType = weeklyType;
            }

            return configuration;

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

                if (templateConfiguration.DefaultVariables.TryGetValue($"__{ parameterName }", out string? value))
                    return value;

                return null;
            }

            string? GetQuotedStringParameter(string? parameterName)
            {
                var value = GetStringParameter(parameterName);
                if (value.IsNullOrEmpty())
                    return null;

                if ((value[0] == '"') && (value[^1] == '"'))
                    value = value[1..^1];

                return value;
            }
        }
    }
}
