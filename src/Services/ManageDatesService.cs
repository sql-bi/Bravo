namespace Sqlbi.Bravo.Services
{
    using Dax.Template.Model;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Infrastructure.Services.ManageDates;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.ManageDates;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public interface IManageDatesService
    {
        IEnumerable<DateConfiguration> GetConfigurations(PBIDesktopReport report, CancellationToken cancellationToken);

        DateConfiguration ValidateConfiguration(PBIDesktopReport report, DateConfiguration configuration, CancellationToken cancellationToken);

        ModelChanges? GetPreviewChanges(PBIDesktopReport report, PreviewChangesSettings settings, CancellationToken cancellationToken);

        void Update(PBIDesktopReport report, DateConfiguration configuration, CancellationToken cancellationToken);
    }

    internal class ManageDatesService : IManageDatesService
    {
        private readonly DaxTemplateManager _templateManager;

        public ManageDatesService()
        {
            _templateManager = new DaxTemplateManager();
        }

        public IEnumerable<DateConfiguration> GetConfigurations(PBIDesktopReport report, CancellationToken cancellationToken)
        {
            var configurations = _templateManager.GetPackages().Select(DateConfiguration.CreateFrom).ToArray();
            {
                ValidateReferencedTables(report, configurations, assertValidation: false);
            }
            return configurations;
        }

        public DateConfiguration ValidateConfiguration(PBIDesktopReport report, DateConfiguration configuration, CancellationToken cancellationToken)
        {
            ValidateReferencedTables(report, configuration, assertValidation: false);

            return configuration;
        }

        public ModelChanges? GetPreviewChanges(PBIDesktopReport report, PreviewChangesSettings settings, CancellationToken cancellationToken)
        {
            BravoUnexpectedException.ThrowIfNull(settings.Configuration);
            ValidateReferencedTables(report, settings.Configuration, assertValidation: true);

            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var modelChanges = _templateManager.GetPreviewChanges(settings.Configuration, settings.PreviewRows, connection, cancellationToken);

            return modelChanges;
        }

        public void Update(PBIDesktopReport report, DateConfiguration configuration, CancellationToken cancellationToken)
        {
            ValidateReferencedTables(report, configuration, assertValidation: true);

            using var connection = TabularConnectionWrapper.ConnectTo(report);

            _templateManager.ApplyTemplate(configuration, connection, cancellationToken);
        }

        private static void ValidateReferencedTables(PBIDesktopReport report, DateConfiguration configuration, bool assertValidation) => ValidateReferencedTables(report, new[] { configuration }, assertValidation);

        private static void ValidateReferencedTables(PBIDesktopReport report, DateConfiguration[] configurations, bool assertValidation)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);

            foreach (var configuration in configurations)
            {
                if (configuration.Bravo?.ReferencedTables is not null)
                {
                    foreach (var referencedTable in configuration.Bravo.ReferencedTables)
                    {
                        var table = connection.Model.Tables.Find(referencedTable.Name);

                        if (table is null)
                        {
                            referencedTable.Action = ReferencedTableAction.ValidCreateNew;
                        }
                        else if (table.IsCalculated())
                        {
                            referencedTable.Action = ReferencedTableAction.ValidOverwrite;
                        }
                        else
                        {
                            referencedTable.Action = ReferencedTableAction.InvalidRenameRequired;
                        }

                        if (assertValidation)
                        {
                            BravoUnexpectedException.Assert(referencedTable.Action != ReferencedTableAction.InvalidRenameRequired);
                        }
                    }
                }
            }

            //foreach (var configuration in configurations)
            //{
            //    if (configuration.Bravo?.ReferencedTables is not null /* && Uri.TryCreate(configuration.TemplateUri, UriKind.Absolute, out var templateUri) */)
            //    {
            //        var packageText = File.ReadAllText(templateUri.LocalPath);
            //        var packageObject = NJ.Linq.JObject.Parse(packageText);

            //        foreach (var referencedTable in configuration.Bravo.ReferencedTables)
            //        {
            //            if (referencedTable.Paths is not null)
            //            {
            //                foreach (var path in referencedTable.Paths)
            //                {
            //                    var token = packageObject.SelectToken(path, errorWhenNoMatch: true);

            //                    BravoUnexpectedException.ThrowIfNull(token);
            //                    BravoUnexpectedException.Assert(token.Type == NJ.Linq.JTokenType.String);

            //                    var xx = token.ToString();
            //                }
            //            }
            //        }
            //    }
            //}
        }
    }
}
