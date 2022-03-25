﻿namespace Sqlbi.Bravo.Infrastructure.Services.ManageDates
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
        public const string SqlbiTemplateAnnotation = "SQLBI_Template";
        public const string SqlbiTemplateAnnotationDatesValue = "Dates";
        public const string SqlbiTemplateAnnotationHolidaysValue = "Holidays";
        public const string SqlbiTemplateTableAnnotation = "SQLBI_TemplateTable";
        public const string SqlbiTemplateTableAnnotationDateValue = "Date";
        public const string SqlbiTemplateTableAnnotationDateAutoTemplateValue = "DateAutoTemplate";
        public const string SqlbiTemplateTableAnnotationHolidaysValue = "Holidays";
        public const string SqlbiTemplateTableAnnotationHolidaysDefinitionValue = "HolidaysDefinition";

        private readonly string EmbeddedPath = Path.Combine(AppContext.BaseDirectory, @"Assets\ManageDates\Templates");
        private readonly string CachePath = Path.Combine(AppEnvironment.ApplicationTempPath, @"ManageDates\Templates");
        private readonly string UserPath = Path.Combine(AppEnvironment.ApplicationDataPath, @"ManageDates\Templates");

        private static readonly object _cacheSyncLock = new();
        private bool _cacheInitialized = false;
        
        public IEnumerable<Package> GetPackages()
        {
            EnsureCalcheInitialized();
            try
            {
                var files = Package.FindTemplateFiles(CachePath);
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
            EnsureCalcheInitialized();
            try
            {
                var package = configuration.GetPackage();
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
            EnsureCalcheInitialized();
            try
            {
                var package = configuration.GetPackage();
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

        private void EnsureCalcheInitialized()
        {
            if (!_cacheInitialized)
            {
                lock (_cacheSyncLock)
                {
                    if (!_cacheInitialized)
                    {
                        if (Directory.Exists(CachePath))
                            Directory.Delete(CachePath, recursive: true);

                        Directory.CreateDirectory(CachePath);
                        
                        var assetPath = Directory.Exists(UserPath) && Directory.EnumerateFiles(UserPath).Any() ? UserPath : EmbeddedPath;

                        foreach (var assetFile in Directory.EnumerateFiles(assetPath))
                        {
                            var assetFileName = Path.GetFileName(assetFile);
                            var cacheFile = Path.Combine(CachePath, assetFileName);

                            File.Copy(assetFile, cacheFile);
                        }

                        _cacheInitialized = true;
                    }
                }
            }
        }
    }
}
