﻿namespace Sqlbi.Bravo.Models.ManageDates
{
    using Dax.Template.Enums;
    using Dax.Template.Exceptions;
    using Dax.Template.Interfaces;
    using Dax.Template.Tables;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Services.DaxTemplate;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using TOM = Microsoft.AnalysisServices.Tabular;

    [DebuggerDisplay("{Name} {IsCurrent}")]
    public class DateConfiguration
    {
        internal const string ExtendedPropertyName = "SQLBI_BRAVO_ManageDatesConfiguration";

        internal static readonly JsonSerializerOptions ExtendedPropertyJsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        public DateConfiguration()
        {
            IsCurrent = false;
            IsCustom = false;
        }

        /// <summary>
        /// For internal use only, not to be shown in Bravo UI
        /// </summary>
        [Required]
        [JsonPropertyName("templateUri")]
        public string? TemplateUri { get; set; }

        /// <summary>
        /// Returns true if this <see cref="DateConfiguration"/> is the one currently applied to the model
        /// </summary>
        [JsonPropertyName("isCurrent")]
        public bool IsCurrent { get; private set; } = false;

        /// <summary>
        /// Returns true if this <see cref="DateConfiguration"/> belongs to a custom developed template and not a predefined Bravo template
        /// </summary>
        [JsonPropertyName("isCustom")]
        public bool IsCustom { get; set; } = false;

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
        /// Indicates whether the user has enabled the creation of the holidays table
        /// </summary>
        [Required]
        [JsonPropertyName("holidaysEnabled")]
        public bool HolidaysEnabled { get; set; } = false;

        [JsonPropertyName("holidaysTableName")]
        public string? HolidaysTableName { get; set; }

        [Required]
        [JsonPropertyName("holidaysTableValidation")]
        public TableValidation HolidaysTableValidation { get; set; } = TableValidation.Unknown;

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
            if (OnlyTablesColumns?.Length > 0) templateConfiguration.OnlyTablesColumns = OnlyTablesColumns;
            if (ExceptTablesColumns?.Length > 0) templateConfiguration.ExceptTablesColumns = ExceptTablesColumns;
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
            // ITemplates.TemplateEntry
            //
            var templateEntries = templateConfiguration.GetTemplateEntries();
            //
            // ITemplates.TemplateEntry - Date (Dax.Template.Tables.Dates.CustomDateTable)
            //
            if (templateEntries.Date is not null)
            {
                if (templateEntries.Date.IsEnabled = DateEnabled)
                {
                    DateTableValidation.Assert();
                    DateReferenceTableValidation.Assert();

                    templateEntries.Date.Table = DateTableName;
                    templateEntries.Date.ReferenceTable = DateReferenceTableName;
                }
            }
            //
            // ITemplates.TemplateEntry - Holidays (Dax.Template.Tables.Dates.HolidaysTable + Dax.Template.Tables.Dates.HolidaysDefinitionTable)
            // 
            if (templateEntries.Holidays is not null)
            {
                BravoUnexpectedException.ThrowIfNull(templateEntries.HolidaysDefinition);

                if (templateEntries.Holidays.IsEnabled = templateEntries.HolidaysDefinition.IsEnabled = HolidaysEnabled)
                {
                    HolidaysTableValidation.Assert();
                    HolidaysDefinitionTableValidation.Assert();
                    BravoUnexpectedException.ThrowIfNull(templateConfiguration.HolidaysReference);

                    templateEntries.Holidays.Table = templateConfiguration.HolidaysReference.TableName = HolidaysTableName;
                    templateEntries.HolidaysDefinition.Table = templateConfiguration.HolidaysDefinitionTable = HolidaysDefinitionTableName;
                }
            }
            //
            // ITemplates.TemplateEntry - TimeIntelligence (Dax.Template.Measures.MeasuresTemplateDefinition.MeasureTemplate)
            //
            if (templateEntries.TimeIntelligence is not null)
            {
                if (templateEntries.TimeIntelligence.IsEnabled = TimeIntelligenceEnabled)
                {
                    // nothing to do
                }
            }
        }

        public static DateConfiguration CreateFrom(Dax.Template.Package package)
        {
            var templateEntries = package.Configuration.GetTemplateEntries();

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
                // ITemplates.TemplateEntry - enable/disable
                //
                DateAvailable = templateEntries.Date is not null,
                DateTableName = templateEntries.Date?.Table,
                DateReferenceTableName = templateEntries.Date?.ReferenceTable,
                //--
                HolidaysAvailable = templateEntries.Holidays is not null && templateEntries.HolidaysDefinition is not null,
                HolidaysTableName = templateEntries.Holidays?.Table,
                HolidaysDefinitionTableName = templateEntries.HolidaysDefinition?.Table,
                //--
                TimeIntelligenceAvailable = templateEntries.TimeIntelligence is not null,
            };

            return configuration;
        }

        public static DateConfiguration? GetCurrentFrom(TOM.Model model)
        {
            var property = model.ExtendedProperties.Find(ExtendedPropertyName);

            if (property is not null && property is TOM.JsonExtendedProperty jsonProperty)
            {
                var configuration = JsonSerializer.Deserialize<DateConfiguration>(jsonProperty.Value, ExtendedPropertyJsonOptions);
                if (configuration is not null)
                {
                    var datesTemplateTableCount = 0;
                    var holidaysTemplateTableCount = 0;

                    #region Update TableName properties from TOM Model

                    // Update the configuration.[Date/Holidays]TableName properties from the connected TOM Model by searching based on annotations
                    // This ensures that the names in the configuration are correct even if the table has been renamed manually or with another tool
                    // We do not update the name property in case more than one table of each type is detected because the Dax.Template library will raise an exception 

                    if (configuration.DateAvailable && configuration.DateEnabled)
                    {
                        // search all tables where annotations 'SQLBI_Template = Dates'
                        var datesTemplateTables = model.Tables.FindByAnnotation(DaxTemplateManager.SqlbiTemplateAnnotation, DaxTemplateManager.SqlbiTemplateAnnotationDatesValue).ToArray();
                        if (datesTemplateTables.Length > 0)
                        {
                            // filter where annotation 'SQLBI_TemplateTable = Date'
                            var datesTemplateDateTables = datesTemplateTables.FindByAnnotation(DaxTemplateManager.SqlbiTemplateTableAnnotation, DaxTemplateManager.SqlbiTemplateTableAnnotationDateValue).Take(2).ToArray();
                            if (datesTemplateDateTables.Length == 1)
                                configuration.DateTableName = datesTemplateDateTables[0].Name;

                            // filter where annotation 'SQLBI_TemplateTable = DateAutoTemplate'
                            var datesTemplateDateReferenceTables = datesTemplateTables.FindByAnnotation(DaxTemplateManager.SqlbiTemplateTableAnnotation, DaxTemplateManager.SqlbiTemplateTableAnnotationDateAutoTemplateValue).Take(2).ToArray();
                            if (datesTemplateDateReferenceTables.Length == 1)
                                configuration.DateReferenceTableName = datesTemplateDateReferenceTables[0].Name;
                        }
                        datesTemplateTableCount = datesTemplateTables.Length;
                    }

                    if (configuration.HolidaysAvailable && configuration.HolidaysEnabled)
                    {
                        // search all tables where annotation 'SQLBI_Template = Holidays'
                        var holidaysTemplateTables = model.Tables.FindByAnnotation(DaxTemplateManager.SqlbiTemplateAnnotation, DaxTemplateManager.SqlbiTemplateAnnotationHolidaysValue).ToArray();
                        if (holidaysTemplateTables.Length > 0)
                        {
                            // filter where annotation 'SQLBI_TemplateTable = Holidays'
                            var holidaysTemplateHolidaysTables = holidaysTemplateTables.FindByAnnotation(DaxTemplateManager.SqlbiTemplateTableAnnotation, DaxTemplateManager.SqlbiTemplateTableAnnotationHolidaysValue).Take(2).ToArray();
                            if (holidaysTemplateHolidaysTables.Length == 1)
                                configuration.HolidaysTableName = holidaysTemplateHolidaysTables[0].Name;

                            // filter where annotation 'SQLBI_TemplateTable = HolidaysDefinition'
                            var holidaysTemplateHolidaysDefinitionTables = holidaysTemplateTables.FindByAnnotation(DaxTemplateManager.SqlbiTemplateTableAnnotation, DaxTemplateManager.SqlbiTemplateTableAnnotationHolidaysDefinitionValue).Take(2).ToArray();
                            if (holidaysTemplateHolidaysDefinitionTables.Length == 1)
                                configuration.HolidaysDefinitionTableName = holidaysTemplateHolidaysDefinitionTables[0].Name;
                        }
                        holidaysTemplateTableCount = holidaysTemplateTables.Length;
                    }

                    #endregion

                    if (datesTemplateTableCount == 0 && holidaysTemplateTableCount == 0)
                    {
                        // Ignore the current configuration if the user deleted all tables created via Dax.Template
                        return null;
                    }

                    configuration.IsCurrent = true;
                    return configuration;
                }
            }

            return null;
        }
    }

    internal static class DateConfigurationExtensions
    {
        public static Dax.Template.Package LoadPackage(this DateConfiguration configuration, bool configure = true)
        {
            BravoUnexpectedException.ThrowIfNull(configuration.TemplateUri);

            {
                // >> HACK
                // versions 0.9.0 to 0.9.3 - TemplateUri format is %LOCALAPPDATA%\[name].template.json
                // versions 0.9.4 to 0.9.5 - TemplateUri format is                [name].template.json

                if (Uri.TryCreate(configuration.TemplateUri, UriKind.Absolute, out _) == false)
                {
                    // If TemplateUri does not contain an absolute URI, we forcibly create one using a known valid local path
                    configuration.TemplateUri = Path.Combine(DaxTemplateManager.CachePath, configuration.TemplateUri);
                }
                // << HACK
            }

            var templateUri = new Uri(configuration.TemplateUri, UriKind.Absolute);
            var templatePath = templateUri.LocalPath;

            if (configuration.IsCustom)
            {
                if (!File.Exists(templatePath) && UserPreferences.Current.CustomOptions is not null)
                {
                    var templatesOptionFound = UserPreferences.Current.CustomOptions.Value.TryGetProperty("templates", out var templatesOption);

                    if (AppEnvironment.IsDiagnosticLevelVerbose)
                        AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{nameof(DateConfigurationExtensions)}.{nameof(LoadPackage)}.CustomOptions.Templates[{templatesOption.ValueKind}]", content: templatesOption.ToString());

                    if (templatesOptionFound)
                    {
                        var customPackages = templatesOption.Deserialize<CustomPackage[]>() ?? Array.Empty<CustomPackage>();
                        var userPackages = customPackages.Where((p) => p.HasPackage && p.Type == CustomPackageType.User && p.Name == configuration.Name).ToList();
                        if (userPackages.Count == 1)
                        {
                            var path = userPackages[0].Path;
                            if (path is not null)
                                templatePath = path;
                        }
                    }
                }
            }
            else
            {
                // Ignore the TemplateUri path and force the current user's local cache path
                templatePath = Path.Combine(DaxTemplateManager.CachePath, Path.GetFileName(templatePath));
            }

            Dax.Template.Package package;
            try
            {
                package = Dax.Template.Package.LoadFromFile(templatePath);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                throw new TemplateException($"The '{configuration.Name}' template file could not be found.", ex);
            }

            if (configure)
            {
                configuration.CopyTo(package.Configuration);
            }

            return package;
        }

        public static void SerializeTo(this DateConfiguration configuration, TOM.Model model)
        {
            var configurationString = JsonSerializer.Serialize(configuration, DateConfiguration.ExtendedPropertyJsonOptions);
            var configurationProperty = model.ExtendedProperties.Find(DateConfiguration.ExtendedPropertyName);

            if (configurationProperty is null)
            {
                var jsonProperty = new TOM.JsonExtendedProperty
                {
                    Name = DateConfiguration.ExtendedPropertyName,
                    Value = configurationString,
                };

                model.ExtendedProperties.Add(jsonProperty);
            }
            else
            {
                BravoUnexpectedException.Assert(configurationProperty is TOM.JsonExtendedProperty);

                var jsonProperty = (TOM.JsonExtendedProperty)configurationProperty;

                if (jsonProperty.Value != configurationString)
                    jsonProperty.Value = configurationString;
            }
        }
    }

    internal static class TemplateConfigurationExtensions
    {
        private const string DateTemplateClassName = "CustomDateTable";
        private const string HolidaysTemplateClassName = "HolidaysTable";
        private const string HolidaysDefinitionTemplateClassName = "HolidaysDefinitionTable";
        private const string TimeIntelligenceTemplateClassName = "MeasuresTemplate";

        public static (ITemplates.TemplateEntry? Date, ITemplates.TemplateEntry? Holidays, ITemplates.TemplateEntry? HolidaysDefinition, ITemplates.TemplateEntry? TimeIntelligence) GetTemplateEntries(this TemplateConfiguration configuration)
        {
            var date = configuration.Templates?.SingleOrDefault((entry) => entry.Class?.Equals(DateTemplateClassName) ?? false);
            var holidays = configuration.Templates?.SingleOrDefault((entry) => entry.Class?.Equals(HolidaysTemplateClassName) ?? false);
            var holidaysDefinition = configuration.Templates?.SingleOrDefault((entry) => entry.Class?.Equals(HolidaysDefinitionTemplateClassName) ?? false);
            var timeIntelligence = configuration.Templates?.SingleOrDefault((entry) => entry.Class?.Equals(TimeIntelligenceTemplateClassName) ?? false);

            return (date, holidays, holidaysDefinition, timeIntelligence);
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
        public static void Assert(this TableValidation value)
        {
            BravoUnexpectedException.Assert(value != TableValidation.Unknown);
            BravoUnexpectedException.Assert(value != TableValidation.InvalidExists);
            BravoUnexpectedException.Assert(value != TableValidation.InvalidNamingRequirements);
        }
    }
}
