namespace Sqlbi.Bravo.Infrastructure.Services.ManageDates
{
    using Dax.Template;
    using Dax.Template.Exceptions;
    using Dax.Template.Model;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Models.ManageDates;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    internal class DaxTemplateManager
    {
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
                var packages = Package.FindTemplateFiles(CachePath).Select(Package.LoadFromFile);
                return packages;
            }
            catch (TemplateException ex)
            {
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
                var modelChanges = Engine.GetModelChanges(connectionWrapper.Model, cancellationToken);

                if (previewRows > 0)
                {
                    using var connection = connectionWrapper.CreateConnection();
                    modelChanges.PopulatePreview(connection, connectionWrapper.Model, previewRows, cancellationToken);
                }

                return modelChanges;
            }
            catch (TemplateException ex)
            {
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

                if (connection.Model.HasLocalChanges)
                    connection.Model.SaveChanges().ThrowOnError();
            }
            catch (TemplateException ex)
            {
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
