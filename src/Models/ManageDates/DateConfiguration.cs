namespace Sqlbi.Bravo.Models.ManageDates
{
    using Dax.Template.Enums;
    using Dax.Template.Interfaces;
    using Dax.Template.Tables;
    using Sqlbi.Bravo.Infrastructure;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class DateConfiguration
    {
        /// <summary>
        /// For internal use only, not to be shown in Bravo UI
        /// </summary>
        [Required]
        [JsonPropertyName("templateUri")]
        public string? TemplateUri { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        #region ILocalization

        [JsonPropertyName("isoFormat")]
        public string? IsoFormat { get; set; }

        [JsonPropertyName("isoTranslation")]
        public string? IsoTranslation { get; set; }

        #endregion

        #region IScanConfig

        [JsonPropertyName("autoScan")]
        public AutoScanEnum? AutoScan { get; set; }

        [JsonPropertyName("onlyTablesColumns")]
        public string[]? OnlyTablesColumns { get; set; }

        [JsonPropertyName("exceptTablesColumns")]
        public string[]? ExceptTablesColumns { get; set; }

        #endregion

        #region IHolidaysConfig

        [JsonPropertyName("isoCountry")]
        public string? IsoCountry { get; set; }

        #endregion

        #region IDateTemplateConfig

        [JsonPropertyName("firstYear")]
        public int? FirstYear { get; set; }

        [JsonPropertyName("lastYear")]
        public int? LastYear { get; set; }

        #endregion

        #region IMeasureTemplateConfig

        [JsonPropertyName("autoNaming")]
        public AutoNamingEnum? AutoNaming { get; set; }

        [JsonPropertyName("targetMeasures")]
        public string[]? TargetMeasures { get; set; }

        [JsonPropertyName("tableSingleInstanceMeasures")]
        public string? TableSingleInstanceMeasures { get; set; }

        #endregion

        #region ICustomTableConfig

        [JsonPropertyName("defaults")]
        public DateDefaults? Defaults { get; set; }

        #endregion

        [JsonPropertyName("bravo")]
        public DateBravo? Bravo { get; set; }

        public void CopyTo(TemplateConfiguration templateConfiguration)
        {
            templateConfiguration.TemplateUri = TemplateUri ?? templateConfiguration.TemplateUri;
            templateConfiguration.Name = Name ?? templateConfiguration.Name;
            templateConfiguration.Description = Description ?? templateConfiguration.Description;
            //
            // ILocalization
            //
            templateConfiguration.IsoFormat = IsoFormat ?? templateConfiguration.IsoFormat;
            templateConfiguration.IsoTranslation = IsoTranslation ?? templateConfiguration.IsoTranslation;
            //
            // IScanConfig
            //
            templateConfiguration.AutoScan = AutoScan ?? templateConfiguration.AutoScan;
            if (OnlyTablesColumns?.Length > 0)
                templateConfiguration.OnlyTablesColumns = OnlyTablesColumns;
            if (ExceptTablesColumns?.Length > 0)
                templateConfiguration.ExceptTablesColumns = ExceptTablesColumns;
            //
            // IHolidaysConfig
            //
            templateConfiguration.IsoCountry = IsoCountry ?? templateConfiguration.IsoCountry;
            //
            // IDateTemplateConfig
            //
            if (FirstYear is not null)
            {
                templateConfiguration.FirstYear = FirstYear;
                templateConfiguration.FirstYearMin = FirstYear;
                templateConfiguration.FirstYearMax = FirstYear;
            }
            if (LastYear is not null)
            {
                templateConfiguration.LastYear = LastYear;
                templateConfiguration.LastYearMin = LastYear;
                templateConfiguration.LastYearMax = LastYear;
            }
            //
            // IMeasureTemplateConfig
            //
            templateConfiguration.AutoNaming = AutoNaming ?? templateConfiguration.AutoNaming;
            if (TargetMeasures?.Length > 0)
            {
                templateConfiguration.TargetMeasures = TargetMeasures
                    .Select((name) => new IMeasureTemplateConfig.TargetMeasure { Name = name })
                    .ToArray();
            }
            templateConfiguration.TableSingleInstanceMeasures = TableSingleInstanceMeasures ?? templateConfiguration.TableSingleInstanceMeasures;

            //
            // ICustomTableConfig
            //
            Defaults?.CopyTo(templateConfiguration);
        }

        public static DateConfiguration CreateFrom(Dax.Template.Package package)
        {
            var configuration = new DateConfiguration
            {
                Bravo = TryGetBravoConfig(package),
                TemplateUri = package.Configuration.TemplateUri,
                Name = package.Configuration.Name,
                Description = package.Configuration.Description,
                //
                // ILocalization
                //
                IsoFormat = package.Configuration.IsoFormat,
                IsoTranslation = package.Configuration.IsoTranslation,
                //
                // IScanConfig
                //
                AutoScan = package.Configuration.AutoScan,
                OnlyTablesColumns = package.Configuration.OnlyTablesColumns,
                ExceptTablesColumns = package.Configuration.ExceptTablesColumns,
                //
                // IHolidaysConfig
                //
                IsoCountry = package.Configuration.IsoCountry,
                //
                // IDateTemplateConfig
                //
                FirstYear = package.Configuration.FirstYear,
                LastYear = package.Configuration.LastYear,
                //
                // IMeasureTemplateConfig
                //
                AutoNaming = package.Configuration.AutoNaming,
                TargetMeasures = package.Configuration.TargetMeasures?.Where((measure) => measure.Name is not null).Select((measure) => measure.Name!).ToArray(),
                TableSingleInstanceMeasures = package.Configuration.TableSingleInstanceMeasures,
                //
                // ICustomTableConfig
                //
                Defaults = DateDefaults.CreateFrom(package.Configuration)
            };

            return configuration;

            static DateBravo? TryGetBravoConfig(Dax.Template.Package package)
            {
                var parentElement = package.JsonConfiguration.RootElement;

                if (parentElement.TryGetProperty(Dax.Template.Package.PACKAGE_CONFIG, out var configElement) && configElement.ValueKind == JsonValueKind.Object)
                    parentElement = configElement;

                if (parentElement.TryGetProperty(nameof(Bravo), out var bravoElement) && bravoElement.ValueKind == JsonValueKind.Object)
                {
                    var bravoConfigText = bravoElement.GetRawText();
                    var bravoConfig = JsonSerializer.Deserialize<DateBravo>(bravoConfigText, options: new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });

                    return bravoConfig;
                }

                return null;
            }
        }
    }

    internal static class DateConfigurationExtensions
    {
        public static Dax.Template.Package GetPackage(this DateConfiguration configuration)
        {
            BravoUnexpectedException.ThrowIfNull(configuration.TemplateUri);

            var templateUri = new Uri(configuration.TemplateUri, UriKind.Absolute);
            if (templateUri.Scheme.Equals(Uri.UriSchemeFile))
            {
                var package = Dax.Template.Package.LoadFromFile(templateUri.LocalPath);
                {
                    configuration.CopyTo(package.Configuration);
                }
                return package;
            }

            throw new NotImplementedException();
        }
    }
}
