namespace Sqlbi.Bravo.Services
{
    using Dax.Template.Exceptions;
    using Dax.Template.Model;
    using Microsoft.AnalysisServices.AdomdClient;
    using Microsoft.AspNetCore.Hosting.Server;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Infrastructure.Services.DaxTemplate;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.ManageDates;
    using Sqlbi.Bravo.Models.TemplateDevelopment;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;

    public interface ITemplateDevelopmentService
    {
        bool Enabled { get; }

        IEnumerable<DateConfiguration> GetConfigurations();

        void CreateWorkspace(string path, string name, DateConfiguration configuration);

        void ConfigureWorkspace(string path);

        ModelChanges? GetPreviewChanges(PBIDesktopReport report, WorkspacePreviewChangesSettings settings, CancellationToken cancellationToken);
    }

    internal class TemplateDevelopmentService : ITemplateDevelopmentService
    {
        private const string WorkspaceFolderName = "src";
        private const string WorkspaceGitignoreFileName = ".gitignore";
        private const string WorkspaceConfigFileName = "bravo-config.json";
        private const string VSCodeExtensionsFileName = "extensions.json";
        private const string VSCodeExtensionName = "sqlbi.bravo-date-template";

        private readonly JsonSerializerOptions _serializerOptions;
        private readonly DaxTemplateManager _templateManager;
        private readonly IServer _hostingServer;

        public TemplateDevelopmentService(IServer hostingServer)
        {
            _hostingServer = hostingServer;
            _templateManager = new DaxTemplateManager();
            _serializerOptions = new JsonSerializerOptions(AppEnvironment.DefaultJsonOptions) { WriteIndented = true };
        }

        public bool Enabled => UserPreferences.Current.TemplateDevelopmentEnabled;

        public IEnumerable<DateConfiguration> GetConfigurations()
        {
            var packages = _templateManager.GetPackages();
            var configurations = packages.Select(DateConfiguration.CreateFrom).ToArray();

            return configurations;
        }

        public void CreateWorkspace(string path, string name, DateConfiguration configuration)
        {
            var workspaceName = name.ReplaceInvalidFileNameChars();
            var workspacePath = Path.Combine(path, workspaceName);

            var templateFiles = _templateManager.GetTemplateFiles(configuration);
            {
                var templatePath = Path.Combine(workspacePath, WorkspaceFolderName);
                Directory.CreateDirectory(templatePath);

                templateFiles.ForEach((templateFilePath) =>
                {
                    var templateFileName = Path.GetFileName(templateFilePath);
                    var workspaceTemplateFilePath = Path.Combine(templatePath, templateFileName);

                    File.Copy(templateFilePath, workspaceTemplateFilePath, overwrite: false);
                });
            }

            //var schemaFiles = _templateManager.GetSchemaFiles();
            //{
            //    var schemaPath = Path.Combine(workspacePath, "src\\Schemas");
            //    Directory.CreateDirectory(schemaPath);

            //    schemaFiles.ForEach((schemaFile) =>
            //    {
            //        var workspaceSchemaFilePath = Path.Combine(schemaPath, schemaFile.Name);

            //        using var fileStream = File.Create(workspaceSchemaFilePath);
            //        schemaFile.Content.CopyTo(fileStream);
            //    });
            //}

            // .vscode\extensions.json
            var vscodePath = Path.Combine(workspacePath, ".vscode");
            var vscodeExtensionsFile = Path.Combine(vscodePath, VSCodeExtensionsFileName);
            {
                if (File.Exists(vscodeExtensionsFile) == false)
                {
                    Directory.CreateDirectory(vscodePath);

                    var content = GetVSCodeExtensionsContent();
                    File.WriteAllText(vscodeExtensionsFile, content);
                }
            }

            // <workspace-name>.code-workspace file
            var codeworkspaceName = $"{workspaceName}.code-workspace";
            var codeworkspaceFile = Path.Combine(workspacePath, codeworkspaceName);
            {
                if (File.Exists(codeworkspaceFile) == false)
                {
                    var content = GetVSCodeCodeworkspaceContent();
                    File.WriteAllText(codeworkspaceFile, content);
                }
            }

            // .gitignore
            var gitignoreFile = Path.Combine(workspacePath, WorkspaceGitignoreFileName);
            {
                if (File.Exists(gitignoreFile) == false)
                {
                    var content = GetWorkspaceGitignoreContent();
                    File.WriteAllText(gitignoreFile, content);
                }
            }

            ConfigureWorkspace(workspacePath);
        }

        public void ConfigureWorkspace(string path)
        {
            var workspacePath = path;

            // bravo-config.json
            var configFile = Path.Combine(workspacePath, WorkspaceConfigFileName);
            {
                var configContent = GetWorkspaceConfigContent();
                File.WriteAllText(configFile, configContent);
            }
        }

        public ModelChanges? GetPreviewChanges(PBIDesktopReport report, WorkspacePreviewChangesSettings settings, CancellationToken cancellationToken)
        {
            BravoUnexpectedException.ThrowIfNull(settings.CustomPackagePath);

            using var connection = TabularConnectionWrapper.ConnectTo(report);
            try
            {
                var package = Dax.Template.Package.LoadFromFile(settings.CustomPackagePath);
                var modelChanges = _templateManager.GetPreviewChanges(package, settings.PreviewRows, connection, cancellationToken);

                return modelChanges;
            }
            catch (Exception ex) when (ex is TemplateException || ex is AdomdException)
            {
                throw new BravoException(BravoProblem.TemplateDevelopmentError, ex.Message, ex);
            }
        }

        private string GetWorkspaceConfigContent()
        {
            var config = new
            {
                Address = _hostingServer.GetListeningAddress(),
                Token = AppEnvironment.ApiAuthenticationTokenTemplateDevelopment,
            };

            var content = JsonSerializer.Serialize(config, _serializerOptions);
            return content;
        }

        private string GetWorkspaceGitignoreContent()
        {
            var content = $@"
# Bravo workspace config file
{WorkspaceConfigFileName}
";
            return content;
        }

        private string GetVSCodeExtensionsContent()
        {
            var extensions = new
            {
                Recommendations = new[]
                {
                    VSCodeExtensionName
                },
            };

            var content = JsonSerializer.Serialize(extensions , _serializerOptions);
            return content;
        }

        private string GetVSCodeCodeworkspaceContent()
        {
            var codeworkspace = new
            {
                Folders = new[]
                {
                    new 
                    {
                        Path = WorkspaceFolderName
                    }
                }
            };

            var content = JsonSerializer.Serialize(codeworkspace, _serializerOptions);
            return content;
        }
    }
}