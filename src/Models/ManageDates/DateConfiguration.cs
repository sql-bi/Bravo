namespace Sqlbi.Bravo.Models.ManageDates
{
    using Dax.Template.Enums;
    using Dax.Template.Tables;
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

        public DateDefaults? Defaults { get; set; }

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
                AutoNaming = templateConfiguration.AutoNaming,

                Defaults = DateDefaults.CreateFrom(templateConfiguration)
            };

            return configuration;
        }
    }
}
