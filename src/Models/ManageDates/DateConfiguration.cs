namespace Sqlbi.Bravo.Models.ManageDates
{
    using Dax.Template.Enums;
    using Dax.Template.Interfaces;
    using Dax.Template.Tables;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Linq;
    using System.Text.Json.Serialization;

    public class DateConfiguration
    {
        internal const string DateTemplateClassName = "CustomDateTable";
        internal const string HolidaysTemplateClassName = "HolidaysTable";
        internal const string HolidaysDefinitionTemplateClassName = "HolidaysDefinitionTable";
        internal const string TimeIntelligenceTemplateClassName = "MeasuresTemplate";

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

        #region Dax.Template.ILocalization

        [JsonPropertyName("isoFormat")]
        public string? IsoFormat { get; set; }

        [JsonPropertyName("isoTranslation")]
        public string? IsoTranslation { get; set; }

        #endregion

        #region Dax.Template.IScanConfig

        [JsonPropertyName("autoScan")]
        public AutoScanEnum? AutoScan { get; set; }

        [JsonPropertyName("onlyTablesColumns")]
        public string[]? OnlyTablesColumns { get; set; }

        [JsonPropertyName("exceptTablesColumns")]
        public string[]? ExceptTablesColumns { get; set; }

        #endregion

        #region Dax.Template.IHolidaysConfig

        [JsonPropertyName("isoCountry")]
        public string? IsoCountry { get; set; }

        #endregion

        #region Dax.Template.IDateTemplateConfig

        [JsonPropertyName("firstYear")]
        public int? FirstYear { get; set; }

        [JsonPropertyName("lastYear")]
        public int? LastYear { get; set; }

        #endregion

        #region Dax.Template.IMeasureTemplateConfig

        [JsonPropertyName("autoNaming")]
        public AutoNamingEnum? AutoNaming { get; set; }

        [JsonPropertyName("targetMeasures")]
        public string[]? TargetMeasures { get; set; }

        [JsonPropertyName("tableSingleInstanceMeasures")]
        public string? TableSingleInstanceMeasures { get; set; }

        #endregion

        #region Dax.Template.ICustomTableConfig

        [JsonPropertyName("defaults")]
        public DateDefaults? Defaults { get; set; }

        #endregion

        #region Date (Dax.Template.Tables.Dates.CustomDateTable)

        /// <summary>
        /// Indicates whether the <see cref="ITemplates.TemplateEntry"/> exists in the config.template.json
        /// </summary>
        [Required]
        [JsonPropertyName("dateAvailable")]
        public bool DateAvailable { get; set; } = false;

        /// <summary>
        /// Indicates whether the user has enabled this template for deploy
        /// </summary>
        [Required]
        [JsonPropertyName("dateEnabled")]
        public bool DateEnabled { get; set; } = false;

        [Required]
        [JsonPropertyName("dateTableName")]
        public string? DateTableName { get; set; }

        [Required]
        [JsonPropertyName("dateTableValidation")]
        public TableValidation DateTableValidation { get; set; } = TableValidation.Unknown;

        [Required]
        [JsonPropertyName("dateReferenceTableName")]
        public string? DateReferenceTableName { get; set; }

        [Required]
        [JsonPropertyName("dateReferenceTableValidation")]
        public TableValidation DateReferenceTableValidation { get; set; } = TableValidation.Unknown;

        #endregion

        #region Holidays (Dax.Template.Tables.Dates.HolidaysTable + Dax.Template.Tables.Dates.HolidaysDefinitionTable)

        /// <summary>
        /// Indicates whether the <see cref="ITemplates.TemplateEntry"/> exists in the config.template.json
        /// </summary>
        [Required]
        [JsonPropertyName("holidaysAvailable")]
        public bool HolidaysAvailable { get; set; } = false;

        /// <summary>
        /// Indicates whether the user has enabled this template for deploy
        /// </summary>
        [Required]
        [JsonPropertyName("holidaysEnabled")]
        public bool HolidaysEnabled { get; set; } = true;

        [Required]
        [JsonPropertyName("holidaysTableName")]
        public string? HolidaysTableName { get; set; }

        [Required]
        [JsonPropertyName("holidaysTableValidation")]
        public TableValidation HolidaysTableValidation { get; set; } = TableValidation.Unknown;

        [Required]
        [JsonPropertyName("holidaysDefinitionTableName")]
        public string? HolidaysDefinitionTableName { get; set; }

        [Required]
        [JsonPropertyName("holidaysDefinitionTableValidation")]
        public TableValidation HolidaysDefinitionTableValidation { get; set; } = TableValidation.Unknown;

        #endregion

        #region TimeIntelligence (Dax.Template.Measures.MeasuresTemplateDefinition.MeasureTemplate)

        /// <summary>
        /// Indicates whether the <see cref="ITemplates.TemplateEntry"/> exists in the config.template.json
        /// </summary>
        [Required]
        [JsonPropertyName("timeIntelligenceAvailable")]
        public bool TimeIntelligenceAvailable { get; set; } = false;

        /// <summary>
        /// Indicates whether the user has enabled this template for deploy
        /// </summary>
        [Required]
        [JsonPropertyName("timeIntelligenceEnabled")]
        public bool TimeIntelligenceEnabled { get; set; } = true;

        #endregion

        public void CopyTo(TemplateConfiguration templateConfiguration)
        {
            templateConfiguration.TemplateUri = TemplateUri ?? templateConfiguration.TemplateUri;
            templateConfiguration.Name = Name ?? templateConfiguration.Name;
            templateConfiguration.Description = Description ?? templateConfiguration.Description;
            //
            // Dax.Template.ILocalization
            //
            templateConfiguration.IsoFormat = IsoFormat ?? templateConfiguration.IsoFormat;
            templateConfiguration.IsoTranslation = IsoTranslation ?? templateConfiguration.IsoTranslation;
            //
            // Dax.Template.IScanConfig
            //
            templateConfiguration.AutoScan = AutoScan ?? templateConfiguration.AutoScan;
            if (OnlyTablesColumns?.Length > 0)
                templateConfiguration.OnlyTablesColumns = OnlyTablesColumns;
            if (ExceptTablesColumns?.Length > 0)
                templateConfiguration.ExceptTablesColumns = ExceptTablesColumns;
            //
            // Dax.Template.IHolidaysConfig
            //
            templateConfiguration.IsoCountry = IsoCountry ?? templateConfiguration.IsoCountry;
            //
            // Dax.Template.IDateTemplateConfig
            //
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
            // Dax.Template.Dax.Template.IMeasureTemplateConfig
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
            // Dax.Template.ICustomTableConfig
            //
            Defaults?.CopyTo(templateConfiguration);
            //
            // Date (Dax.Template.Tables.Dates.CustomDateTable)
            //
            var dateTemplateEntry = templateConfiguration.Templates?.SingleOrDefault((entry) => entry.Class?.Equals(DateTemplateClassName) ?? false);
            {
                if (dateTemplateEntry is not null)
                {
                    if (DateEnabled)
                    {
                        BravoUnexpectedException.Assert(DateTableValidation.IsValid());
                        BravoUnexpectedException.Assert(DateReferenceTableValidation.IsValid());

                        dateTemplateEntry.Table = DateTableName;
                        dateTemplateEntry.ReferenceTable = DateReferenceTableName;
                    }
                    else
                    {
                        templateConfiguration.Templates = templateConfiguration.Templates!.Except(new[] { dateTemplateEntry }).ToArray();
                    }
                }
            }
            //
            // Holidays (Dax.Template.Tables.Dates.HolidaysTable + Dax.Template.Tables.Dates.HolidaysDefinitionTable)
            //
            var holidaysTemplateEntry = templateConfiguration.Templates?.SingleOrDefault((entry) => entry.Class?.Equals(HolidaysTemplateClassName) ?? false);
            var holidaysDefinitionTemplateEntry = templateConfiguration.Templates?.SingleOrDefault((entry) => entry.Class?.Equals(HolidaysDefinitionTemplateClassName) ?? false);
            {
                if (holidaysTemplateEntry is not null)
                {
                    BravoUnexpectedException.ThrowIfNull(holidaysDefinitionTemplateEntry);

                    if (HolidaysEnabled)
                    {
                        BravoUnexpectedException.Assert(HolidaysTableValidation.IsValid());
                        BravoUnexpectedException.Assert(HolidaysDefinitionTableValidation.IsValid());
                        BravoUnexpectedException.ThrowIfNull(templateConfiguration.HolidaysReference);

                        holidaysTemplateEntry.Table = templateConfiguration.HolidaysReference.TableName = HolidaysTableName;
                        holidaysDefinitionTemplateEntry.Table = templateConfiguration.HolidaysDefinitionTable = HolidaysDefinitionTableName;
                    }
                    else
                    {
                        templateConfiguration.Templates = templateConfiguration.Templates!.Except(new[] { holidaysTemplateEntry, holidaysDefinitionTemplateEntry }).ToArray();
                        
                        // HACK >> to fix TemplateException($"Holidays table '{config.HolidaysReference?.TableName}' not found.");
                        templateConfiguration.HolidaysReference = null;
                        templateConfiguration.HolidaysDefinitionTable = null;
                        // HACK <<
                    }
                }
            }
            //
            // Time Intelligence (Dax.Template.Measures.MeasuresTemplateDefinition.MeasureTemplate)
            //
            var timeintelligenceTemplateEntry = templateConfiguration.Templates?.SingleOrDefault((entry) => entry.Class?.Equals(TimeIntelligenceTemplateClassName) ?? false);
            {
                if (timeintelligenceTemplateEntry is not null)
                {
                    if (TimeIntelligenceEnabled)
                    {
                        // nothing to do
                    }
                    else
                    {
                        templateConfiguration.Templates = templateConfiguration.Templates!.Except(new[] { timeintelligenceTemplateEntry }).ToArray();
                    }
                }
            }
        }

        public static DateConfiguration CreateFrom(Dax.Template.Package package)
        {
            var dateTemplateEntry = package.Configuration.Templates?.SingleOrDefault((entry) => entry.Class?.Equals(DateTemplateClassName) ?? false);
            var holidaysTemplateEntry = package.Configuration.Templates?.SingleOrDefault((entry) => entry.Class?.Equals(HolidaysTemplateClassName) ?? false);
            var holidaysDefinitionTemplateEntry = package.Configuration.Templates?.SingleOrDefault((entry) => entry.Class?.Equals(HolidaysDefinitionTemplateClassName) ?? false);
            var timeintelligenceTemplateEntry = package.Configuration.Templates?.SingleOrDefault((entry) => entry.Class?.Equals(TimeIntelligenceTemplateClassName) ?? false);

            var configuration = new DateConfiguration
            {
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
                Defaults = DateDefaults.CreateFrom(package.Configuration),
                //
                // Enable disable templates
                //
                DateAvailable = dateTemplateEntry is not null,
                DateTableName = dateTemplateEntry?.Table,
                DateReferenceTableName = dateTemplateEntry?.ReferenceTable,
                //--
                HolidaysAvailable = holidaysTemplateEntry is not null && holidaysDefinitionTemplateEntry is not null,
                HolidaysTableName = holidaysTemplateEntry?.Table,
                HolidaysDefinitionTableName = holidaysDefinitionTemplateEntry?.Table,
                //--
                TimeIntelligenceAvailable = timeintelligenceTemplateEntry is not null,
            };

            return configuration;
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
                var customizedPackagePath = Path.ChangeExtension(templateUri.LocalPath, $"{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}.json");

                var package = Dax.Template.Package.LoadFromFile(templateUri.LocalPath);
                {
                    configuration.CopyTo(package.Configuration);
                    package.SaveTo(customizedPackagePath);
                }

                var customizedPackage = Dax.Template.Package.LoadFromFile(customizedPackagePath);
                return customizedPackage;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    public enum TableValidation
    {
        Unknown = 0,

        /// <summary>
        /// A table with the same name does not exist and will be created
        /// </summary>
        ValidNotExists = 1,

        /// <summary>
        /// A table with the same name already exists but will be altered
        /// </summary>
        ValidAlterable = 2,

        /// <summary>
        /// A table with the same name already exists and cannot be altered, a different name is required
        /// </summary>
        InvalidExists = 100,

        /// <summary>
        /// The table name contains words or characters that cannot be used in the name of a table
        /// </summary>
        InvalidNamingRequirements = 101,
    }

    internal static class TableValidationExtensions
    {
        public static bool IsValid(this TableValidation value) => value > TableValidation.Unknown && value < TableValidation.InvalidExists;
    }
}
