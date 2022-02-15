namespace Sqlbi.Bravo.Services
{
    using Dax.Template;
    using Dax.Template.Model;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.ManageDates;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using TOM = Microsoft.AnalysisServices.Tabular;

    public interface IManageDatesService
    {
        IEnumerable<DateConfiguration> GetConfigurations();

        ModelChanges? GetPreviewChanges(PBIDesktopReport report, PreviewChangesSettings settings, CancellationToken cancellationToken);

        void Update(PBIDesktopReport report, DateConfiguration configuration, CancellationToken cancellationToken);
    }

    internal class ManageDatesService : IManageDatesService
    {
        public IEnumerable<DateConfiguration> GetConfigurations()
        {
            var path = Path.Combine(AppContext.BaseDirectory, @"Assets\ManageDates\Templates");

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


        public ModelChanges? GetPreviewChanges(PBIDesktopReport report, PreviewChangesSettings settings, CancellationToken cancellationToken)
        {
            var modelChanges = ApplyTemplates(report, settings.Configuration!, previewChanges: true, settings.PreviewRows, cancellationToken);
            return modelChanges;
        }

        public void Update(PBIDesktopReport report, DateConfiguration configuration, CancellationToken cancellationToken)
        {
            _ = ApplyTemplates(report, configuration, previewChanges: false, previewRows: 0, cancellationToken);
        }

        private static ModelChanges? ApplyTemplates(PBIDesktopReport report, DateConfiguration configuration, bool previewChanges, int previewRows, CancellationToken cancellationToken)
        {
            BravoUnexpectedException.ThrowIfNull(report.ServerName);
            BravoUnexpectedException.ThrowIfNull(report.DatabaseName);
            BravoUnexpectedException.ThrowIfNull(configuration.TemplateUri);

            var connectionString = ConnectionStringHelper.BuildForPBIDesktop(report.ServerName);
            var databaseName = report.DatabaseName;

            Package? package;
            Uri templateUri = new(configuration.TemplateUri, UriKind.Absolute);

            if (templateUri.Scheme.Equals(Uri.UriSchemeFile))
            {
                package = Package.LoadFromFile(templateUri.LocalPath);
            }
            else
            {
                throw new NotImplementedException();
            }

            configuration.CopyTo(package.Configuration);
            var engine = new Engine(package);

            using var server = new TOM.Server();
            server.Connect(connectionString);

            var database = server.Databases.FindByName(databaseName) ?? throw new BravoException(BravoProblem.TOMDatabaseDatabaseNotFound, databaseName);
            var model = database.Model;

            engine.ApplyTemplates(model);

            if (previewChanges)
            {
                var modelChanges = engine.GetModelChanges(model);

                if (previewRows > 0)
                    modelChanges.PopulatePreview(model, previewRows);

                return modelChanges;
            }
            else
            {
                // TODO: handle SaveChanges() errors
                model.SaveChanges();

                return null;
            }
        }
    }
}
