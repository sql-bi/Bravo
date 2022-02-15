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

        #region Localization

        [JsonPropertyName("isoFormat")]
        public string? IsoFormat { get; set; }

        [JsonPropertyName("isoTranslation")]
        public string? IsoTranslation { get; set; }

        #endregion

        #region Scan

        [JsonPropertyName("autoScan")]
        public AutoScanEnum? AutoScan { get; set; }

        [JsonPropertyName("onlyTablesColumns")]
        public string[]? OnlyTablesColumns { get; set; }

        [JsonPropertyName("exceptTablesColumns")]
        public string[]? ExceptTablesColumns { get; set; }

        #endregion

        #region Holidays

        [JsonPropertyName("isoCountry")]
        public string? IsoCountry { get; set; }

        [JsonPropertyName("firstYear")]
        public int? FirstYear { get; set; }

        [JsonPropertyName("lastYear")]
        public int? LastYear { get; set; }

        #endregion

        #region MeasureTemplate

        [JsonPropertyName("autoNaming")]
        public AutoNamingEnum? AutoNaming { get; set; }

        [JsonPropertyName("targetMeasures")]
        public string[]? TargetMeasures { get; set; }

        [JsonPropertyName("tableSingleInstanceMeasures")]
        public string? TableSingleInstanceMeasures { get; set; }

        #endregion

        #region CustomDateTable

        [JsonPropertyName("defaults")]
        public DateDefaults? Defaults { get; set; }

        #endregion

        public void CopyTo(TemplateConfiguration templateConfiguration)
        {
            templateConfiguration.TemplateUri ??= TemplateUri;
            templateConfiguration.Name ??= Name;
            templateConfiguration.Description ??= Description;
            //
            // Localization
            //
            templateConfiguration.IsoFormat ??= IsoFormat;
            templateConfiguration.IsoTranslation ??= IsoTranslation;
            //
            // Scan
            //
            templateConfiguration.AutoScan ??= AutoScan;
            if (OnlyTablesColumns?.Length > 0)
            {
                templateConfiguration.OnlyTablesColumns = OnlyTablesColumns;
            }
            if (ExceptTablesColumns?.Length > 0)
            {
                templateConfiguration.ExceptTablesColumns = ExceptTablesColumns;
            }
            //
            // Holidays
            //
            templateConfiguration.IsoCountry ??= IsoCountry;
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
            //
            // MeasureTemplate
            //
            templateConfiguration.AutoNaming = AutoNaming ?? templateConfiguration.AutoNaming;
            if (TargetMeasures?.Length > 0)
            {
                templateConfiguration.TargetMeasures = TargetMeasures.Select((name) => new IMeasureTemplateConfig.TargetMeasure { Name = name }).ToArray();
            }
            // ???? TableSingleInstanceMeasures
            //
            // CustomDateTable
            //
            Defaults?.CopyTo(templateConfiguration);
        }

        public static DateConfiguration CreateFrom(TemplateConfiguration templateConfiguration)
        {
            var configuration = new DateConfiguration
            {
                TemplateUri = templateConfiguration.TemplateUri,
                Name = templateConfiguration.Name,
                Description = templateConfiguration.Description,
                //
                // Localization
                //
                IsoFormat = templateConfiguration.IsoFormat,
                IsoTranslation = templateConfiguration.IsoTranslation,
                //
                // Scan
                //
                AutoScan = templateConfiguration.AutoScan,
                // ???? OnlyTablesColumns
                // ???? ExceptTablesColumns
                //
                // Holidays
                //
                IsoCountry = templateConfiguration.IsoCountry,
                // ???? FirstYear
                // ???? LastYear
                //
                // MeasureTemplate
                //
                AutoNaming = templateConfiguration.AutoNaming,
                // ???? TargetMeasures
                // ???? TableSingleInstanceMeasures
                //
                // CustomDateTable
                //
                Defaults = DateDefaults.CreateFrom(templateConfiguration)
            };

            return configuration;
        }
    }
}
