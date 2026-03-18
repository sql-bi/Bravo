namespace Sqlbi.Bravo.Infrastructure.Services.DaxTemplate
{
    using Dax.Template;
    using Dax.Template.Model;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Security.Policies;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.ManageDates;

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

        internal static readonly string CachePath = Path.Combine(AppEnvironment.ApplicationTempPath, @"ManageDates\Templates");
        internal static readonly string UserPath = Path.Combine(AppEnvironment.ApplicationDataPath, @"ManageDates\Templates");

        private readonly object _cacheSyncLock = new();

        public DaxTemplateManager()
        {
            InitializeCache();
        }

        public Package GetPackage(string path)
        {
            return Package.LoadFromFile(path);
        }

        public IEnumerable<Package> GetPackages()
        {
            if (BravoPolicies.Current.BuiltInTemplatesEnabledPolicy == PolicyStatus.Forced)
            {
                if (BravoPolicies.Current.BuiltInTemplatesEnabled == false)
                {
                    return Array.Empty<Package>();
                }
            }

            var files = Package.FindTemplateFiles(CachePath);
            return files.Select(Package.LoadFromFile).ToArray();
        }

        public ModelChanges GetPreviewChanges(DateConfiguration configuration, int previewRows, TabularConnectionWrapper connection, CancellationToken cancellationToken)
        {
            var package = configuration.LoadPackage();
            return GetPreviewChanges(package, previewRows, connection, cancellationToken);
        }

        public ModelChanges GetPreviewChanges(Package package, int previewRows, TabularConnectionWrapper connection, CancellationToken cancellationToken)
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

        public void ApplyConfiguration(DateConfiguration configuration, TabularConnectionWrapper connection, CancellationToken cancellationToken)
        {
            var package = configuration.LoadPackage();
            var engine = new Engine(package);

            engine.ApplyTemplates(connection.Model, cancellationToken);
            configuration.SerializeTo(connection.Model);
            connection.Model.SaveChanges().ThrowOnError();
        }

        private IEnumerable<(string Name, Stream Content)> GetSchemaFiles()
        {
            var files = GetResourceFiles(resourcePrefix: SchemaEmbeddedResourcePrefix);
            return files;
        }

        private IEnumerable<(string Name, Stream Content)> GetTemplateFiles()
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
                        if (Directory.Exists(CachePath))
                            Directory.Delete(CachePath, recursive: true);

                        Directory.CreateDirectory(CachePath);
                        
                        if (Directory.Exists(UserPath))
                        {
                            // If the path exists then we use the templates from this folder instead of using the built-in default templates - this is for testing/debug purpose only
                            foreach (var assetFile in Directory.EnumerateFiles(UserPath))
                            {
                                var assetFileName = Path.GetFileName(assetFile);
                                var cacheFile = Path.Combine(CachePath, assetFileName);

                                File.Copy(assetFile, cacheFile);
                            }
                        }
                        else
                        {
                            var templateFiles = GetTemplateFiles();

                            foreach (var templateFile in templateFiles)
                            {
                                var templateFilePath = Path.Combine(CachePath, templateFile.Name);

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
