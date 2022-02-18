namespace Sqlbi.Bravo.Infrastructure.Services.ManageDates
{
    using Dax.Template;
    using Dax.Template.Model;
    using Sqlbi.Bravo.Models.ManageDates;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    internal class DaxTemplateManager
    {
        private readonly string AssetsPath = Path.Combine(AppContext.BaseDirectory, @"Assets\ManageDates\Templates");
        private readonly string CachePath = Path.Combine(AppEnvironment.ApplicationTempPath, @"ManageDates\Templates");

        private static readonly object _cacheSyncLock = new();
        private bool _cacheInitialized = false;
        
        public IEnumerable<Package> GetPackages()
        {
            EnsureCalcheInitialized();

            var packages = Package.FindTemplateFiles(CachePath).Select(Package.LoadFromFile);
            return packages;
        }

        public ModelChanges GetPreviewChanges(DateConfiguration configuration, int previewRows, TabularConnectionWrapper connection, CancellationToken cancellationToken)
        {
            EnsureCalcheInitialized();

            var package = configuration.GetPackage();
            var engine = new Engine(package);

            engine.ApplyTemplates(connection.Model, cancellationToken);

            var modelChanges = engine.GetModelChanges(connection.Model, cancellationToken);
            {
                if (previewRows > 0)
                {
                    modelChanges.PopulatePreview(connection.Model, previewRows, cancellationToken);
                }
            }
            return modelChanges;
        }

        public void ApplyTemplate(DateConfiguration configuration, TabularConnectionWrapper connection, CancellationToken cancellationToken)
        {
            EnsureCalcheInitialized();

            var package = configuration.GetPackage();
            var engine = new Engine(package);

            engine.ApplyTemplates(connection.Model, cancellationToken);

            // TODO: handle SaveChanges() errors
            connection.Model.SaveChanges();
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
                        
                        foreach (var assetFile in Directory.EnumerateFiles(AssetsPath))
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
