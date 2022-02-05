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
    using DaxTemplate = Dax.Template;

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
                var configurations = DaxTemplate.Package.FindTemplates(path).Select(DaxTemplate.Package.LoadFrom).Select((package) =>
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
            var path = configuration.TemplateId ?? throw new BravoUnexpectedException("configuration.TemplateId is null");

            var package = DaxTemplate.Package.LoadFrom(path);
            {
                configuration.CopyTo(package.Configuration);
            }
            var engine = new DaxTemplate.Engine(package);

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
