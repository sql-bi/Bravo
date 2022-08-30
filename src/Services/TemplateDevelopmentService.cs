namespace Sqlbi.Bravo.Services
{
    using Dax.Template.Exceptions;
    using Dax.Template.Model;
    using Microsoft.AnalysisServices.AdomdClient;
    using Microsoft.AspNetCore.Hosting.Server;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
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
        IEnumerable<DateConfiguration> GetConfigurations();

        DateConfiguration? GetConfigurationFromPackage(string path);

        CustomPackage GetUserCustomPackage(string path);

        CustomPackage ValidateCustomPackage(CustomPackage customPackage);

        CustomPackage CreateWorkspace(string path, string name, DateConfiguration configuration);

        bool ConfigureWorkspace(string path, bool openCodeWorkspace);

        ModelChanges? GetPreviewChanges(PBIDesktopReport report, WorkspacePreviewChangesSettings settings, CancellationToken cancellationToken);
    }

    internal class TemplateDevelopmentService : ITemplateDevelopmentService
    {
        private const string WorkspaceSourceFolderName = "src";
        private const string WorkspaceDistributionFolderName = "dist";
        private const string WorkspaceGitignoreFileName = ".gitignore";
        private const string WorkspaceConfigFileName = "bravo-config.json";
        private const string VSCodeExtensionsFileName = "extensions.json";
        private const string VSCodeExtensionName = "sqlbi.bravo-template-editor";

        private readonly JsonSerializerOptions _serializerOptions;
        private readonly DaxTemplateManager _templateManager;
        private readonly IServer _hostingServer;

        public TemplateDevelopmentService(IServer hostingServer)
        {
            _hostingServer = hostingServer;
            _templateManager = new DaxTemplateManager();
            _serializerOptions = new JsonSerializerOptions(AppEnvironment.DefaultJsonOptions) { WriteIndented = true };
        }
        public IEnumerable<DateConfiguration> GetConfigurations()
        {
            var packages = _templateManager.GetPackages();
            var configurations = packages.Select(DateConfiguration.CreateFrom).ToArray();

            return configurations;
        }

        public DateConfiguration? GetConfigurationFromPackage(string path)
        {
            if (File.Exists(path))
            {
                var package = _templateManager.GetPackage(path);
                var configuration = DateConfiguration.CreateFrom(package);

                configuration.IsCustom = true;
                return configuration;
            }

            return null;
        }

        public CustomPackage GetUserCustomPackage(string path)
        {
            var customPackage = new CustomPackage
            {
                Type = CustomPackageType.User,
                Path = null,
                Name = null,
                Description = null,
                WorkspaceName = null,
                WorkspacePath = null,
            };
            var extension = Path.GetExtension(path);

            if (extension.EqualsI(".json") && path.EndsWithI(".package.json"))
            {
                SetPackageFileDetails(path, customPackage);

                var packageFileInfo = new FileInfo(path);
                if (packageFileInfo.Directory?.Parent is not null)
                {
                    var workspaceName = packageFileInfo.Directory.Parent.Name;
                    var workspaceFolder = packageFileInfo.Directory.Parent.FullName;
                    var workspaceCodeworkspaceName = Path.ChangeExtension(workspaceName, ".code-workspace");
                    var workspaceCodeworkspacePath = Path.Combine(workspaceFolder, workspaceCodeworkspaceName);

                    SetPackageWorkspaceDetails(workspaceCodeworkspacePath, customPackage);
                }
            }
            else if (extension.EqualsI(".code-workspace"))
            {
                SetPackageWorkspaceDetails(path, customPackage);

                var codeworkspaceFileInfo = new FileInfo(path);
                if (codeworkspaceFileInfo.Directory is not null)
                {
                    var workspaceName = codeworkspaceFileInfo.Directory.Name;
                    var pacakgeFileName = Path.ChangeExtension(workspaceName, ".package.json");
                    var packageFilePath = Path.Combine(codeworkspaceFileInfo.Directory.FullName, WorkspaceDistributionFolderName, pacakgeFileName);

                    SetPackageFileDetails(packageFilePath, customPackage);
                }
            }

            return customPackage;

            void SetPackageFileDetails(string path, CustomPackage customPackage)
            {
                if (File.Exists(path))
                {
                    var package = _templateManager.GetPackage(path);

                    customPackage.Path = path;
                    customPackage.Name = package.Configuration.Name;
                    customPackage.Description = package.Configuration.Description;
                    customPackage.HasPackage = true;
                }
            }

            void SetPackageWorkspaceDetails(string path, CustomPackage customPackage)
            {
                var codeworkspaceFileInfo = new FileInfo(path);
                if (codeworkspaceFileInfo.Exists && codeworkspaceFileInfo.Directory is not null)
                {
                    customPackage.WorkspaceName = codeworkspaceFileInfo.Directory.Name;
                    customPackage.WorkspacePath = codeworkspaceFileInfo.Directory.FullName;
                    customPackage.HasWorkspace = true;
                }
            }
        }

        public CustomPackage ValidateCustomPackage(CustomPackage customPackage)
        {
            customPackage.HasWorkspace = false;
            customPackage.HasPackage = false;

            if (customPackage.Path is not null)
            {
                var existingCustomPackage = GetUserCustomPackage(customPackage.Path);

                if (customPackage.HasWorkspace == false && existingCustomPackage.HasWorkspace == true)
                {
                    customPackage.WorkspaceName = existingCustomPackage.WorkspaceName;
                    customPackage.WorkspacePath = existingCustomPackage.WorkspacePath;
                }

                customPackage.HasWorkspace = existingCustomPackage.HasWorkspace;
                customPackage.HasPackage = existingCustomPackage.HasPackage;
            }
            else if (customPackage.WorkspacePath is not null && customPackage.WorkspaceName is not null)
            {
                var workspaceCodeworkspaceName = Path.ChangeExtension(customPackage.WorkspaceName, ".code-workspace");
                var workspaceCodeworkspacePath = Path.Combine(customPackage.WorkspacePath, workspaceCodeworkspaceName);
                var existingCustomPackage = GetUserCustomPackage(workspaceCodeworkspacePath);

                if (customPackage.HasPackage == false && existingCustomPackage.HasPackage == true)
                {
                    customPackage.Path = existingCustomPackage.Path;
                    customPackage.Name = existingCustomPackage.Name;
                    customPackage.Description = existingCustomPackage.Description;
                }

                customPackage.HasWorkspace = existingCustomPackage.HasWorkspace;
                customPackage.HasPackage = existingCustomPackage.HasPackage;
            }

            return customPackage;
        }

        public CustomPackage CreateWorkspace(string path, string name, DateConfiguration configuration)
        {
            BravoUnexpectedException.Assert(configuration.IsCustom == false);

            var workspaceName = name.ReplaceInvalidFileNameChars();
            var workspacePath = Path.Combine(path, workspaceName);
            var customPackage = new CustomPackage
            {
                Type = CustomPackageType.User,
                Path = null,
                Name = name,
                Description = null,
                WorkspaceName = workspaceName,
                WorkspacePath = workspacePath,
                HasWorkspace = true,
                HasPackage = false,
            };
            var package = configuration.LoadPackage(configure: false);

            // src\*.json files
            {
                var templatePath = Path.Combine(workspacePath, WorkspaceSourceFolderName);
                Directory.CreateDirectory(templatePath);

                BravoUnexpectedException.ThrowIfNull(package.Configuration.TemplateUri);
                {
                    package.Configuration.Name = customPackage.Name;
                    package.Configuration.Description = customPackage.Description;

                    var configJson = JsonSerializer.Serialize(package.Configuration, new JsonSerializerOptions() { WriteIndented = true });
                    var configPath = Path.Combine(templatePath, Path.GetFileName(package.Configuration.TemplateUri));
                    File.WriteAllText(configPath, configJson);
                }

                if (package.Configuration.Templates is not null)
                {
                    foreach (var template in package.Configuration.Templates)
                    {
                        if (template.Template is not null)
                        {
                            var sourcePath = Path.Combine(DaxTemplateManager.CachePath, template.Template);
                            var destinationPath = Path.Combine(templatePath, template.Template);
                            File.Copy(sourcePath, destinationPath, overwrite: false);
                        }
                    }
                }

                if (package.Configuration.LocalizationFiles is not null)
                {
                    foreach (var localizationFile in package.Configuration.LocalizationFiles)
                    {
                        var sourcePath = Path.Combine(DaxTemplateManager.CachePath, localizationFile);
                        var destinationPath = Path.Combine(templatePath, localizationFile);
                        File.Copy(sourcePath, destinationPath, overwrite: false);
                    }
                }
            }

            // dist\[name].package.json
            {
                var packageDistributionFolderPath = Path.Combine(workspacePath, WorkspaceDistributionFolderName);
                var packageFileName = Path.ChangeExtension(workspaceName, ".package.json");
                var packageFilePath = Path.Combine(packageDistributionFolderPath, packageFileName);

                Directory.CreateDirectory(packageDistributionFolderPath);
                package.SaveTo(packageFilePath);

                customPackage.Path = packageFilePath;
                customPackage.HasPackage = true;
            }

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

            // <workspace-name>.code-workspace
            var codeworkspaceName = Path.ChangeExtension(workspaceName, ".code-workspace");
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

            // bravo-config.json
            var configFile = Path.Combine(workspacePath, WorkspaceConfigFileName);
            {
                if (File.Exists(configFile) == false)
                {
                    File.WriteAllText(configFile, string.Empty);
                }
            }

            return customPackage;
        }

        public bool ConfigureWorkspace(string path, bool openCodeWorkspace)
        {
            var workspacePath = path;

            // bravo-config.json
            var configFile = Path.Combine(workspacePath, WorkspaceConfigFileName);
            {
                if (File.Exists(configFile) == false)
                {
                    return false; // Not a workspace folder
                }

                var configContent = GetWorkspaceConfigContent();
                File.WriteAllText(configFile, configContent);
            }

            if (openCodeWorkspace)
            {
                var codeworkspaceFiles = Directory.GetFiles(workspacePath, "*.code-workspace", new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = false,
                });

                if (codeworkspaceFiles.Length == 1)
                {
                    _ = ProcessHelper.OpenShellExecute(codeworkspaceFiles[0], waitForStarted: false, out var _);
                }
            }

            return true;
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
            var content = $@"# Bravo workspace config file
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
                        Path = "."
                    }
                }
            };

            var content = JsonSerializer.Serialize(codeworkspace, _serializerOptions);
            return content;
        }
    }
}