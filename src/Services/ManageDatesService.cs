namespace Sqlbi.Bravo.Services
{
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Models.ManageDates;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using DaxTemplate = Dax.Template;

    public interface IManageDatesService
    {
        IEnumerable<DateConfiguration> GetConfigurations();
    }

    internal class ManageDatesService : IManageDatesService
    {
        /// <summary>
        /// Retrieve all the <see cref="DateConfiguration"/> from the Dax.Templates templates available
        /// </summary>
        public IEnumerable<DateConfiguration> GetConfigurations()
        {
            var path = UserPreferences.Current.ManageDatesTemplatePath ?? Path.Combine(Infrastructure.AppEnvironment.ApplicationDataPath, "ManageDates\\Templates");
         
            if (Directory.Exists(path))
            {
                var configurations = DaxTemplate.Package.FindTemplates(path).Select(DaxTemplate.Package.Load).Select((package) =>
                {
                    var configuration = DateConfiguration.CreateFrom(package.Configuration);
                    return configuration;
                });

                return configurations.ToArray();
            }

            return Array.Empty<DateConfiguration>();
        }

        /*
                /// <summary>
                /// Apply template or just preview result for a template with a specific configuration
                /// </summary>
                /// <param name="config">Template and configuration</param>
                /// <param name="model">Model on which the template is applied</param>
                /// <param name="connectionString">Connection string for Adomd used to preview changes</param>
                /// <param name="commitChanges">TRUE to commit changes</param>
                /// <param name="previewRows">Number of rows to include in data preview</param>
                /// <returns>Changes applied to the model</returns>
                /// <exception cref="Exception">Errors executing template</exception>
                public ModelChanges? ApplyTemplate(DateConfiguration config, Model model, string connectionString, bool commitChanges, int previewRows = 5)
                {
                    var package = DaxTemplate.Package.Load(config.TemplatePath);

                    CopyConfiguration();

                    var engine = new DaxTemplate.Engine(package);
                    engine.ApplyTemplates(model);

                    var modelChanges = DaxTemplate.Engine.GetModelChanges(model);

                    if (commitChanges)
                    {
                        model.SaveChanges();
                    }
                    else
                    {
                        // Only for preview data
                        AdomdConnection connection = new(connectionString);
                        modelChanges.PopulatePreview(connection, model, previewRows);
                    }

                    return modelChanges;

                    void CopyConfiguration()
                    {
                        package.Configuration.IsoCountry = config.IsoCountry ?? package.Configuration.IsoCountry;
                        package.Configuration.IsoFormat = config.IsoFormat ?? package.Configuration.IsoFormat;
                        package.Configuration.IsoTranslation = config.IsoTranslation ?? package.Configuration.IsoTranslation;
                        package.Configuration.AutoScan = config.AutoScan ?? package.Configuration.AutoScan;
                        package.Configuration.AutoNaming = config.AutoNaming ?? package.Configuration.AutoNaming;

                        SetIntVariable(nameof(config.Defaults.FirstFiscalMonth), config.Defaults.FirstFiscalMonth);
                        SetIntVariable(nameof(config.Defaults.FirstDayOfWeek), (int?)config.Defaults.FirstDayOfWeek);
                        SetIntVariable(nameof(config.Defaults.MonthsInYear), config.Defaults.MonthsInYear);
                        SetStringVariable(nameof(config.Defaults.WorkingDayType), config.Defaults.WorkingDayType);
                        SetStringVariable(nameof(config.Defaults.NonWorkingDayType), config.Defaults.NonWorkingDayType);
                        SetIntVariable(nameof(config.Defaults.TypeStartFiscalYear), (int?)config.Defaults.TypeStartFiscalYear);
                        SetStringVariable(nameof(config.Defaults.QuarterWeekType), (int?)config.Defaults.QuarterWeekType);
                        SetStringVariable(nameof(config.Defaults.WeeklyType), config.Defaults.WeeklyType);

                        if (config.FirstYear != null)
                        {
                            package.Configuration.FirstYear = (int)config.FirstYear;
                            package.Configuration.FirstYearMin = (int)config.FirstYear;
                            package.Configuration.FirstYearMax = (int)config.FirstYear;
                        }

                        if (config.LastYear != null)
                        {
                            package.Configuration.LastYear = (int)config.LastYear;
                            package.Configuration.LastYearMin = (int)config.LastYear;
                            package.Configuration.LastYearMax = (int)config.LastYear;
                        }

                        if (config.OnlyTablesColumns?.Length > 0)
                        {
                            package.Configuration.OnlyTablesColumns = config.OnlyTablesColumns.ToArray();
                        }

                        if (config.ExceptTablesColumns?.Length > 0)
                        {
                            package.Configuration.ExceptTablesColumns = config.ExceptTablesColumns.ToArray();
                        }

                        if (config.TargetMeasures?.Length > 0)
                        {
                            var targetMeasures =
                                from measureName in config.TargetMeasures
                                select new Dax.Template.Interfaces.IMeasureTemplateConfig.TargetMeasure() { Name = measureName };

                            package.Configuration.TargetMeasures = targetMeasures.ToArray();
                        }
                    }

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
                        if ((value == null) || (package == null))
                            return;

                        string key = $"__{parameterName}";

                        if (!package.Configuration.DefaultVariables.ContainsKey(key))
                        {
                            throw new Exception($"Invalid {key} variable.");
                        }

                        string? variableValue = value.ToString();

                        if (variableValue == null)
                        {
                            throw new Exception($"Null value for {key} variable.");
                        }

                        package.Configuration.DefaultVariables[key] = $"{quote}{variableValue}{quote}";
                    }
                }
        */
    }
}
