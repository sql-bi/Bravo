namespace Sqlbi.Bravo.Services
{
    using Dax.Template;
    using Dax.Template.Exceptions;
    using Dax.Template.Model;
    using Microsoft.AnalysisServices.AdomdClient;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Infrastructure.Services.DaxTemplate;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.ManageDates;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    public interface IManageDatesService
    {
        IEnumerable<CustomPackage> GetCustomPackages(CancellationToken cancellationToken);

        IEnumerable<DateConfiguration> GetConfigurations(PBIDesktopReport report, CancellationToken cancellationToken);

        DateConfiguration ValidateConfiguration(PBIDesktopReport report, DateConfiguration configuration, CancellationToken cancellationToken);

        ModelChanges? GetPreviewChanges(PBIDesktopReport report, PreviewChangesSettings settings, CancellationToken cancellationToken);

        ModelChanges? GetPreviewChanges(PBIDesktopReport report, CustomPackagePreviewChangesSettings settings, CancellationToken cancellationToken);

        void ApplyTemplate(PBIDesktopReport report, DateConfiguration configuration, CancellationToken cancellationToken);

        void ApplyTemplate(PBIDesktopReport report, CustomPackage customPackage, CancellationToken cancellationToken);
    }

    internal class ManageDatesService : IManageDatesService
    {
        private readonly DaxTemplateManager _templateManager;

        public ManageDatesService()
        {
            _templateManager = new DaxTemplateManager();
        }

        public IEnumerable<CustomPackage> GetCustomPackages(CancellationToken cancellationToken)
        {
            var userPackages = GetImpl(UserPreferences.Current.ManageDatesPackageRepository, CustomPackageType.User).ToArray();
            //var organizationPackages = Get(???, CustomPackageType.User).ToArray();

            return userPackages;

            IEnumerable<CustomPackage> GetImpl(string? path, CustomPackageType type)
            {
                if (Directory.Exists(path))
                {
                    var packageFiles = Directory.EnumerateFiles(path, "*.package.json", new EnumerationOptions
                    {
                        IgnoreInaccessible = true,
                        RecurseSubdirectories = true,
                    });

                    foreach (var packageFile in packageFiles)
                    {
                        var package = _templateManager.GetPackage(packageFile);
                        var customPackage = new CustomPackage
                        {
                            Type = type,
                            Folder = GetFileRelativePath(path, packageFile),
                            Name = package.Configuration.Name,
                            Description = package.Configuration.Description,
                            Path = packageFile,
                        };

                        yield return customPackage;
                    }
                }
            }

            static string? GetFileRelativePath(string relativeTo, string filePath)
            {
                var relativePath = Path.GetRelativePath(relativeTo, filePath);
                var directoryName = Path.GetDirectoryName(relativePath);

                return directoryName;
            }
        }

        public IEnumerable<DateConfiguration> GetConfigurations(PBIDesktopReport report, CancellationToken cancellationToken)
        {
            IEnumerable<Package> packages;
            try
            {
                packages = _templateManager.GetPackages();
            }
            catch (TemplateException ex)
            {
                throw new BravoException(BravoProblem.ManageDateTemplateError, ex.Message, ex);
            }

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
            try
            {
                var modelChanges = _templateManager.GetPreviewChanges(settings.Configuration, settings.PreviewRows, connection, cancellationToken);
                return modelChanges;
            }
            catch (Exception ex) when (ex is TemplateException || ex is AdomdException)
            {
                throw new BravoException(BravoProblem.ManageDateTemplateError, ex.Message, ex);
            }
        }

        public ModelChanges? GetPreviewChanges(PBIDesktopReport report, CustomPackagePreviewChangesSettings settings, CancellationToken cancellationToken)
        {
            BravoUnexpectedException.ThrowIfNull(settings.CustomPackage);

            using var connection = TabularConnectionWrapper.ConnectTo(report);
            try
            {
                var modelChanges = _templateManager.GetPreviewChanges(settings.CustomPackage, settings.PreviewRows, connection, cancellationToken);
                return modelChanges;
            }
            catch (Exception ex) when (ex is TemplateException || ex is AdomdException)
            {
                throw new BravoException(BravoProblem.ManageDateTemplateError, ex.Message, ex);
            }
        }

        public void ApplyTemplate(PBIDesktopReport report, DateConfiguration configuration, CancellationToken cancellationToken)
        {
            Validate(report, configuration, assertValidation: true);

            using var connection = TabularConnectionWrapper.ConnectTo(report);
            try
            {
                _templateManager.ApplyTemplate(configuration, connection, cancellationToken);
            }
            catch (TemplateException ex)
            {
                throw new BravoException(BravoProblem.ManageDateTemplateError, ex.Message, ex);
            }
        }

        public void ApplyTemplate(PBIDesktopReport report, CustomPackage customPackage, CancellationToken cancellationToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            try
            {
                _templateManager.ApplyTemplate(customPackage, connection, cancellationToken);
            }
            catch (TemplateException ex)
            {
                throw new BravoException(BravoProblem.ManageDateTemplateError, ex.Message, ex);
            }
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
