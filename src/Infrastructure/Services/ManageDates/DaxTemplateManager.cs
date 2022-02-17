namespace Sqlbi.Bravo.Infrastructure.Services.ManageDates
{
    using Dax.Template;
    using Dax.Template.Model;
    using Sqlbi.Bravo.Models.ManageDates;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using NJ = Newtonsoft.Json;

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
                        Directory.Delete(CachePath, recursive: true);
                        Directory.CreateDirectory(CachePath);

                        var packages = Package.FindTemplateFiles(AssetsPath).Select(Package.LoadFromFile);

                        foreach (var package in packages)
                            SaveToCache(package);

                        _cacheInitialized = true;
                    }
                }
            }
        }

        private void SaveToCache(Package package)
        {
            BravoUnexpectedException.ThrowIfNull(package.Configuration.Name);

            var cacheName = Path.ChangeExtension(package.Configuration.Name, Package.PACKAGE_FILE_EXTENSION);
            var cachePath = Path.Combine(CachePath, cacheName);
            var templateUri = (new UriBuilder(cachePath) { Scheme = Uri.UriSchemeFile }).Uri.AbsolutePath;

            // TODO: on .NET 6 use System.Text.Json.Nodes.JsonNode 
            // JsonNode and the classes that derive from it provide the ability to create a mutable DOM
            // Meanwhile that we are on .NET 5 we can use Newtonsoft.Json as workaround 

            var cachedPackageObject = new NJ.Linq.JObject();
            {
                var configurationText = JsonSerializer.Serialize(package.JsonConfiguration);
                var configurationObject = NJ.Linq.JObject.Parse(configurationText);
                {
                    configurationObject.AddFirst(new NJ.Linq.JProperty(name: nameof(package.Configuration.Name), content: package.Configuration.Name));
                    configurationObject.AddFirst(new NJ.Linq.JProperty(name: nameof(package.Configuration.TemplateUri), content: templateUri));

                    cachedPackageObject.Add(new NJ.Linq.JProperty(Package.PACKAGE_CONFIG, configurationObject));
                }

                var templateFiles = from t in package.Configuration.Templates select t.Template;
                var localizationFiles = from t in package.Configuration.Templates from l in t.LocalizationFiles select l;
                var embeddedFiles = templateFiles.Union(localizationFiles).Where((file) => file is not null).Distinct();
                {
                    foreach (var embeddedFile in embeddedFiles)
                    {
                        var filePath = Path.Combine(AssetsPath, embeddedFile);
                        var fileText = File.ReadAllText(filePath);
                        var content = NJ.Linq.JObject.Parse(fileText);
                        var name = Path.GetFileNameWithoutExtension(embeddedFile);

                        cachedPackageObject.Add(new NJ.Linq.JProperty(name, content));
                    }
                }
            }
            var cachedPackageText = NJ.JsonConvert.SerializeObject(cachedPackageObject, NJ.Formatting.Indented);

            File.WriteAllText(cachePath, cachedPackageText);
        }
    }
}
