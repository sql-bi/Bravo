namespace Sqlbi.Bravo.Models.ManageDates
{
    using Dax.Template.Enums;
    using Dax.Template.Interfaces;
    using Dax.Template.Tables;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Text.Json.Serialization;

    public class DateConfiguration
    {
        /// <summary>
        /// For internal use only, not to be shown in Bravo UI
        /// </summary>
        [Required]
        [JsonPropertyName("templateUri")]
        public string? TemplateUri { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("isoFormat")]
        public string? IsoFormat { get; set; }

        [JsonPropertyName("isoCountry")]
        public string? IsoCountry { get; set; }

        [JsonPropertyName("isoTranslation")]
        public string? IsoTranslation { get; set; }

        [JsonPropertyName("onlyTablesColumns")]
        public string[]? OnlyTablesColumns { get; set; }

        [JsonPropertyName("exceptTablesColumns")]
        public string[]? ExceptTablesColumns { get; set; }

        [JsonPropertyName("tableSingleInstanceMeasures")]
        public string? TableSingleInstanceMeasures { get; set; }

        [JsonPropertyName("targetMeasures")]
        public string[]? TargetMeasures { get; set; }

        [JsonPropertyName("firstYear")]
        public int? FirstYear { get; set; }

        [JsonPropertyName("lastYear")]
        public int? LastYear { get; set; }

        [JsonPropertyName("autoScan")]
        public AutoScanEnum? AutoScan { get; set; }

        [JsonPropertyName("autoNaming")]
        public AutoNamingEnum? AutoNaming { get; set; }

        [JsonPropertyName("defaults")]
        public DateDefaults? Defaults { get; set; }

        //[JsonIgnore]
        //public bool IsFileTemplate => Uri.TryCreate(TemplateUri, UriKind.Absolute, out var uri) && uri.IsFile;

        public void CopyTo(TemplateConfiguration templateConfiguration)
        {
            templateConfiguration.TemplateUri ??= TemplateUri;
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
                TemplateUri = templateConfiguration.TemplateUri,
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
