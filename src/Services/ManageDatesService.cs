namespace Sqlbi.Bravo.Services
{
    using Dax.Template.Model;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Models.ManageDates;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Dax.Template;

    public interface IManageDatesService
    {
        IEnumerable<DateConfiguration> GetConfigurations();

        ModelChanges? Apply(DateConfiguration configuration, bool commitChanges, int previewRows = 5);
    }

    internal class ManageDatesService : IManageDatesService
    {
        public IEnumerable<DateConfiguration> GetConfigurations()
        {
            var path = UserPreferences.Current.ManageDatesTemplatePath ?? Path.Combine(AppEnvironment.ApplicationDataPath, "ManageDates\\Templates");
         
            if (Directory.Exists(path))
            {
                var configurations = Package.FindTemplateFiles(path).Select(Package.LoadFromFile).Select((package) =>
                {
                    var configuration = DateConfiguration.CreateFrom(package.Configuration);
                    return configuration;
                });

                return configurations.ToArray();
            }

            return Array.Empty<DateConfiguration>();
        }

        public ModelChanges? Apply(DateConfiguration configuration, bool commitChanges, int previewRows = 5)
        {
            Package? templatePackage;
            Uri templateUri = new(configuration.TemplateUri!, UriKind.Absolute);

            if (templateUri.Scheme.Equals(Uri.UriSchemeFile))
            {
                templatePackage = Package.LoadFromFile(templateUri.LocalPath);
                configuration.CopyTo(templatePackage.Configuration);
            }
            else
            {
                throw new BravoUnexpectedException("configuration.TemplateUri.Scheme unknown");
            }

            var templateEngine = new Engine(templatePackage);

            //engine.ApplyTemplates(model);

            //var modelChanges = DaxTemplate.Engine.GetModelChanges(model);

            //if (commitChanges)
            //{
            //    model.SaveChanges();
            //}
            //else
            //{
            //    // Only for preview data
            //    AdomdConnection connection = new(connectionString);
            //    modelChanges.PopulatePreview(connection, model, previewRows);
            //}

            //return modelChanges;

            return null;
        }
    }
}
