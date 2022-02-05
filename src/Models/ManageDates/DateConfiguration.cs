namespace Sqlbi.Bravo.Models.ManageDates
{
    using Dax.Template.Enums;
    using Dax.Template.Interfaces;
    using Dax.Template.Tables;
    using System.Linq;
    using System.Text.Json.Serialization;

    public class DateConfiguration
    {
        /// <summary>
        /// For internal use only, not to be shown in Bravo UI
        /// </summary>
        public string? TemplateId { get; init; }

        public string? Name { get; init; }

        public string? Description { get; init; }

        public string? IsoFormat { get; set; }

        public string? IsoCountry { get; set; }

        public string? IsoTranslation { get; set; }

        public string[]? OnlyTablesColumns { get; set; }

        public string[]? ExceptTablesColumns { get; set; }

        public string? TableSingleInstanceMeasures { get; set; }

        public string[]? TargetMeasures { get; set; }

        public int? FirstYear { get; set; }

        public int? LastYear { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AutoScanEnum? AutoScan { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AutoNamingEnum? AutoNaming { get; set; }

        public DateDefaults? Defaults { get; set; }

        public void CopyTo(TemplateConfiguration templateConfiguration)
        {
            templateConfiguration.IsoFormat ??= IsoFormat;
            templateConfiguration.IsoCountry ??= IsoCountry;
            templateConfiguration.IsoTranslation ??= IsoTranslation;
            templateConfiguration.AutoNaming = AutoNaming ?? templateConfiguration.AutoNaming;
            templateConfiguration.AutoScan ??= AutoScan;

            Defaults?.CopyTo(templateConfiguration);

            if (OnlyTablesColumns?.Length > 0)
            {
                templateConfiguration.OnlyTablesColumns = OnlyTablesColumns;
            }

            if (ExceptTablesColumns?.Length > 0)
            {
                templateConfiguration.ExceptTablesColumns = ExceptTablesColumns;
            }

            // ???? TableSingleInstanceMeasures
            
            if (TargetMeasures?.Length > 0)
            {
                templateConfiguration.TargetMeasures = TargetMeasures.Select((name) => new IMeasureTemplateConfig.TargetMeasure { Name = name }).ToArray();
            }

            if (FirstYear.HasValue)
            {
                templateConfiguration.FirstYear = FirstYear.Value;
                templateConfiguration.FirstYearMin = FirstYear.Value;
                templateConfiguration.FirstYearMax = FirstYear.Value;
            }

            if (LastYear.HasValue)
            {
                templateConfiguration.LastYear = LastYear.Value;
                templateConfiguration.LastYearMin = LastYear.Value;
                templateConfiguration.LastYearMax = LastYear.Value;
            }
        }

        public static DateConfiguration CreateFrom(TemplateConfiguration templateConfiguration)
        {
            var configuration = new DateConfiguration
            {
                // TemplatePath = package.Path *** TODO ***
                //TemplateId = templateConfiguration.
                Name = templateConfiguration.Name,
                Description = templateConfiguration.Description,
                IsoFormat = templateConfiguration.IsoFormat,
                IsoCountry = templateConfiguration.IsoCountry,
                IsoTranslation = templateConfiguration.IsoTranslation,

                // ???? OnlyTablesColumns
                // ???? ExceptTablesColumns
                // ???? TableSingleInstanceMeasures
                // ???? TargetMeasures
                // ???? FirstYear
                // ???? LastYear

                AutoScan = templateConfiguration.AutoScan,
                AutoNaming = templateConfiguration.AutoNaming,

                Defaults = DateDefaults.CreateFrom(templateConfiguration)
            };

            return configuration;
        }
    }
}
