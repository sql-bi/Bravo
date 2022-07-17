namespace Sqlbi.Bravo.Infrastructure.Services.DaxTemplate
{
    using Dax.Template;
    using Dax.Template.Exceptions;
    using Dax.Template.Model;
    using Microsoft.AnalysisServices.AdomdClient;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models.ManageDates;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    internal class DaxTemplateManager
    {
        private static bool CacheInitialized = false;

        public const string SqlbiTemplateAnnotation = "SQLBI_Template";
        public const string SqlbiTemplateAnnotationDatesValue = "Dates";
        public const string SqlbiTemplateAnnotationHolidaysValue = "Holidays";
        public const string SqlbiTemplateTableAnnotation = "SQLBI_TemplateTable";
        public const string SqlbiTemplateTableAnnotationDateValue = "Date";
        public const string SqlbiTemplateTableAnnotationDateAutoTemplateValue = "DateAutoTemplate";
        public const string SqlbiTemplateTableAnnotationHolidaysValue = "Holidays";
        public const string SqlbiTemplateTableAnnotationHolidaysDefinitionValue = "HolidaysDefinition";

        private const string TemplateEmbeddedResourcePrefix = "Sqlbi.Bravo.Assets.ManageDates.Templates.";
        private const string SchemaEmbeddedResourcePrefix = "Sqlbi.Bravo.Assets.TemplateDevelopment.Schemas.";

        private readonly string _cachePath = Path.Combine(AppEnvironment.ApplicationTempPath, @"ManageDates\Templates");
        private readonly string _userPath = Path.Combine(AppEnvironment.ApplicationDataPath, @"ManageDates\Templates");
        private readonly object _cacheSyncLock = new();

        public DaxTemplateManager()
        {
            InitializeCache();
        }

        public IEnumerable<Package> GetPackages()
        {
            try
            {
                var files = Package.FindTemplateFiles(_cachePath);
                var packages = files.Select(Package.LoadFromFile).ToArray();

                return packages;
            }
            catch (TemplateException ex)
            {
                TelemetryHelper.TrackException(ex);
                throw;
            }
        }

        public IEnumerable<string> GetTemplateFiles(DateConfiguration configuration)
        {
            var files = new List<string>();

            var package = configuration.LoadPackage(_cachePath);
            {
                if (package.Configuration.TemplateUri is not null)
                {
                    var path = Path.Combine(_cachePath, package.Configuration.TemplateUri);
                    files.Add(path);
                }

                if (package.Configuration?.Templates is not null)
                {
                    foreach (var template in package.Configuration.Templates)
                    {
                        if (template.Template is not null)
                        {
                            var path = Path.Combine(_cachePath, template.Template);
                            files.Add(path);
                        }
                    }
                }

                if (package.Configuration?.LocalizationFiles is not null)
                {
                    foreach (var localizationFile in package.Configuration.LocalizationFiles)
                    {
                        var path = Path.Combine(_cachePath, localizationFile);
                        files.Add(path);
                    }
                }
            }

            return files;
        }

        public ModelChanges GetPreviewChanges(DateConfiguration configuration, int previewRows, TabularConnectionWrapper connection, CancellationToken cancellationToken)
        {
            try
            {
                var package = configuration.GetPackage(_cachePath);
                var modelChanges = GetPreviewChanges(package, previewRows, connection, cancellationToken);

                return modelChanges;
            }
            catch (Exception ex) when (ex is TemplateException || ex is AdomdException)
            {
                TelemetryHelper.TrackException(ex);
                throw;
            }
        }

        public ModelChanges GetPreviewChanges(Package package, int previewRows, TabularConnectionWrapper connection, CancellationToken cancellationToken)
        {
            try
            {
                var engine = new Engine(package);

                engine.ApplyTemplates(connection.Model, cancellationToken);
                try
                {
                    var modelChanges = Engine.GetModelChanges(connection.Model, cancellationToken);

                    if (previewRows > 0)
                    {
                        using var adomdConnection = connection.CreateAdomdConnection();
                        modelChanges.PopulatePreview(adomdConnection, connection.Model, previewRows, cancellationToken);
                    }

                    return modelChanges;
                }
                finally
                {
                    connection.Model.UndoLocalChanges();
                }
            }
            catch (Exception ex) when (ex is TemplateException || ex is AdomdException)
            {
                TelemetryHelper.TrackException(ex);
                throw;
            }
        }

        public void ApplyTemplate(DateConfiguration configuration, TabularConnectionWrapper connection, CancellationToken cancellationToken)
        {
            try
            {
                var package = configuration.GetPackage(_cachePath);
                var engine = new Engine(package);

                engine.ApplyTemplates(connection.Model, cancellationToken);
                configuration.SerializeTo(connection.Model);
                connection.Model.SaveChanges().ThrowOnError();
            }
            catch (TemplateException ex)
            {
                TelemetryHelper.TrackException(ex);
                throw;
            }
        }

        public IEnumerable<(string Name, Stream Content)> GetSchemaFiles()
        {
            var files = GetResourceFiles(resourcePrefix: SchemaEmbeddedResourcePrefix);
            return files;
        }

        public IEnumerable<(string Name, Stream Content)> GetTemplateFiles()
        {
            var files = GetResourceFiles(resourcePrefix: TemplateEmbeddedResourcePrefix);
            return files;
        }

        private IEnumerable<(string Name, Stream Content)> GetResourceFiles(string resourcePrefix)
        {
            var assembly = typeof(Program).Assembly;
            var resourceNames = assembly.GetManifestResourceNames();

            foreach (var resourceName in resourceNames)
            {
                if (resourceName.StartsWith(resourcePrefix))
                {
                    var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream is not null)
                    {
                        var name = resourceName.Remove(0, resourcePrefix.Length);
                        yield return (Name: name, Content: stream);
                    }
                }
            }
        }

        private void InitializeCache()
        {
            if (CacheInitialized == false)
            {
                lock (_cacheSyncLock)
                {
                    if (CacheInitialized == false)
                    {
                        if (Directory.Exists(_cachePath))
                            Directory.Delete(_cachePath, recursive: true);

                        Directory.CreateDirectory(_cachePath);
                        
                        if (Directory.Exists(_userPath))
                        {
                            foreach (var assetFile in Directory.EnumerateFiles(_userPath))
                            {
                                var assetFileName = Path.GetFileName(assetFile);
                                var cacheFile = Path.Combine(_cachePath, assetFileName);

                                File.Copy(assetFile, cacheFile);
                            }
                        }
                        else
                        {
                            var templateFiles = GetTemplateFiles();

                            foreach (var templateFile in templateFiles)
                            {
                                var templateFilePath = Path.Combine(_cachePath, templateFile.Name);

                                using var fileStream = File.Create(templateFilePath);
                                templateFile.Content.CopyTo(fileStream);
                            }
                        }

                        CacheInitialized = true;
                    }
                }
            }
        }
    }
}
