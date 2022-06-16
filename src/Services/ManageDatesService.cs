﻿namespace Sqlbi.Bravo.Services
{
    using Dax.Template.Model;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
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
            var packages = _templateManager.GetPackages();
            var configurations = packages.Select(DateConfiguration.CreateFrom).ToList();

            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var currentConfiguration = DateConfiguration.GetCurrentFrom(connection.Model);

            if (currentConfiguration is not null)
                configurations.Insert(0, currentConfiguration);

            Validate(report, configurations, assertValidation: false);

            return configurations;
        }

        public DateConfiguration ValidateConfiguration(PBIDesktopReport report, DateConfiguration configuration, CancellationToken cancellationToken)
        {
            Validate(report, configuration, assertValidation: false);

            return configuration;
        }

        public ModelChanges? GetPreviewChanges(PBIDesktopReport report, PreviewChangesSettings settings, CancellationToken cancellationToken)
        {
            BravoUnexpectedException.ThrowIfNull(settings.Configuration);
            Validate(report, settings.Configuration, assertValidation: true);

            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var modelChanges = _templateManager.GetPreviewChanges(settings.Configuration, settings.PreviewRows, connection, cancellationToken);

            return modelChanges;
        }

        public void Update(PBIDesktopReport report, DateConfiguration configuration, CancellationToken cancellationToken)
        {
            Validate(report, configuration, assertValidation: true);

            using var connection = TabularConnectionWrapper.ConnectTo(report);

            _templateManager.ApplyTemplate(configuration, connection, cancellationToken);
        }

        private static void Validate(PBIDesktopReport report, DateConfiguration configuration, bool assertValidation) => Validate(report, new[] { configuration }, assertValidation);

        private static void Validate(PBIDesktopReport report, IEnumerable<DateConfiguration> configurations, bool assertValidation)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);

            foreach (var configuration in configurations)
            {
                if (configuration.DateEnabled)
                {
                    configuration.DateTableValidation = Validate(configuration.DateTableName);
                    configuration.DateReferenceTableValidation = Validate(configuration.DateReferenceTableName);

                    if (assertValidation)
                    {
                        configuration.DateTableValidation.Assert();
                        configuration.DateReferenceTableValidation.Assert();
                    }
                }

                if (configuration.HolidaysEnabled)
                {
                    configuration.HolidaysTableValidation = Validate(configuration.HolidaysTableName);
                    configuration.HolidaysDefinitionTableValidation = Validate(configuration.HolidaysDefinitionTableName);

                    if (assertValidation)
                    {
                        configuration.HolidaysTableValidation.Assert();
                        configuration.HolidaysDefinitionTableValidation.Assert();
                    }
                }

                if (configuration.TimeIntelligenceEnabled)
                {
                    // nothing todo
                }
            }

            TableValidation Validate(string? tableName)
            {
                var validation = TableValidation.Unknown;

                if (!TabularModelHelper.IsValidTableName(tableName))
                {
                    validation = TableValidation.InvalidNamingRequirements;
                }
                else
                {
                    var table = connection.Model.Tables.Find(tableName);

                    if (table is null)
                    {
                        validation = TableValidation.ValidNotExists;
                    }
                    else if (table.IsCalculated())
                    {
                        validation = TableValidation.ValidAlterable;
                    }
                    else
                    {
                        validation = TableValidation.InvalidExists;
                    }
                }

                return validation;
            }
        }
    }
}
