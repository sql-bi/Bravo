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
        public const string SqlbiTemplateEmbeddedResourcePrefix = "Sqlbi.Bravo.Assets.ManageDates.Templates.";

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
                throw new BravoException(BravoProblem.ManageDateTemplateError, ex.Message, ex);
            }
        }

        public ModelChanges GetPreviewChanges(DateConfiguration configuration, int previewRows, TabularConnectionWrapper connectionWrapper, CancellationToken cancellationToken)
        {
            try
            {
                var package = configuration.GetPackage(_cachePath);
                var engine = new Engine(package);

                engine.ApplyTemplates(connectionWrapper.Model, cancellationToken);
                try
                {
                    var modelChanges = Engine.GetModelChanges(connectionWrapper.Model, cancellationToken);

                    if (previewRows > 0)
                    {
                        using var connection = connectionWrapper.CreateAdomdConnection();
                        modelChanges.PopulatePreview(connection, connectionWrapper.Model, previewRows, cancellationToken);
                    }

                    return modelChanges;
                }
                finally
                {
                    connectionWrapper.Model.UndoLocalChanges();
                }
            }
            catch (Exception ex) when (ex is TemplateException || ex is AdomdException)
            {
                TelemetryHelper.TrackException(ex);
                throw new BravoException(BravoProblem.ManageDateTemplateError, ex.Message, ex);
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
                throw new BravoException(BravoProblem.ManageDateTemplateError, ex.Message, ex);
            }
        }

        private void InitializeCache()
        {
            if (!CacheInitialized)
            {
                lock (_cacheSyncLock)
                {
                    if (!CacheInitialized)
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
                            var assembly = typeof(Program).Assembly;
                            var resourceNames = assembly.GetManifestResourceNames();
                            var templateNames = resourceNames.Where((name) => name.StartsWith(SqlbiTemplateEmbeddedResourcePrefix));

                            foreach (var templateName in templateNames)
                            {
                                using var resourceStream = assembly.GetManifestResourceStream(templateName);
                                if (resourceStream is not null)
                                {
                                    var cacheFileName = templateName.Remove(0, SqlbiTemplateEmbeddedResourcePrefix.Length);
                                    var cacheFilePath = Path.Combine(_cachePath, cacheFileName);

                                    using var fileStream = File.Create(cacheFilePath);
                                    resourceStream.CopyTo(fileStream);
                                }
                            }
                        }

                        CacheInitialized = true;
                    }
                }
            }
        }
    }
}
