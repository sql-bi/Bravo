namespace Sqlbi.Bravo.Services
{
    using Dax.Template.Exceptions;
    using Dax.Template.Model;
    using Microsoft.AnalysisServices.AdomdClient;
    using Microsoft.AspNetCore.Hosting.Server;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Security;
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

        void UpdateStatus(bool enabled);

        IEnumerable<DateConfiguration> GetTemplateConfigurations();

        void CreateWorkspace(string path, string name, DateConfiguration configuration);

        void ConfigureWorkspace(string path);

        ModelChanges? GetPreviewChanges(PBIDesktopReport report, WorkspacePreviewChangesSettings settings, CancellationToken cancellationToken);
    }

    internal class TemplateDevelopmentService : ITemplateDevelopmentService
    {
        private const string WorkspaceConfigFileName = "bravo-config.json";
        private const string WorkspaceGitignoreFileName = ".gitignore";

        private readonly DaxTemplateManager _templateManager;
        private readonly IServer _hostingServer;

        public TemplateDevelopmentService(IServer hostingServer)
        {
            _hostingServer = hostingServer;
            _templateManager = new DaxTemplateManager();
        }

        public bool Enabled => AppEnvironment.TemplateDevelopmentEnabled;

        public void UpdateStatus(bool enabled)
        {
            AppEnvironment.TemplateDevelopmentEnabled = enabled;
        }

        public IEnumerable<DateConfiguration> GetTemplateConfigurations()
        {
            var packages = _templateManager.GetPackages();
            var configurations = packages.Select(DateConfiguration.CreateFrom).ToArray();

            return configurations;
        }

        public void CreateWorkspace(string path, string name, DateConfiguration configuration)
        {
            var workspaceName = name.ReplaceInvalidFileNameChars();
            var workspacePath = Path.Combine(path, workspaceName);

            Directory.CreateDirectory(workspacePath);

            var templateFiles = _templateManager.GetTemplateFiles(configuration);
            {
                templateFiles.ForEach((templateFilePath) =>
                {
                    var templateFileName = Path.GetFileName(templateFilePath);
                    var workspaceFilePath = Path.Combine(workspacePath, templateFileName);

                    File.Copy(templateFilePath, workspaceFilePath, overwrite: false);
                });
            }

            ConfigureWorkspace(workspacePath);
        }

        public void ConfigureWorkspace(string path)
        {
            var workspacePath = path;
            var configPath = Path.Combine(workspacePath, WorkspaceConfigFileName);
            {
                var configContent = GetWorkspaceConfigContent();
                File.WriteAllText(configPath, configContent);
            }
            var gitignorePath = Path.Combine(workspacePath, WorkspaceGitignoreFileName);
            {
                var gitignoreContent = GetWorkspaceGitignoreContent();
                var gitignoreExists = File.Exists(gitignorePath);

                if (gitignoreExists)
                {
                    var gitignoreContainsConfigFile = File.ReadLines(gitignorePath).Any((line) => line.EqualsTI(WorkspaceConfigFileName));
                    if (gitignoreContainsConfigFile == false)
                    {
                        File.AppendAllText(gitignorePath, gitignoreContent);
                    }
                }
                else
                {
                    File.WriteAllText(gitignorePath, gitignoreContent);
                }
            }
        }

        public ModelChanges? GetPreviewChanges(PBIDesktopReport report, WorkspacePreviewChangesSettings settings, CancellationToken cancellationToken)
        {
            BravoUnexpectedException.ThrowIfNull(settings.Package);

            //Validate(report, settings.Configuration, assertValidation: true);

            using var connection = TabularConnectionWrapper.ConnectTo(report);
            try
            {
                var package = Dax.Template.Package.LoadFromFile(settings.Package);
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
            var workspaceConfig = new
            {
                Address = _hostingServer.GetListeningAddress(),
                Token = AppEnvironment.ApiAuthenticationTokenTemplateDevelopment, //.ToProtectedString(),
                //
                Port = _hostingServer.GetListeningAddress().Port, // TODO: remove and use the address
            };

            var configContent = JsonSerializer.Serialize(workspaceConfig, AppEnvironment.DefaultJsonOptions);
            return configContent;
        }

        private string GetWorkspaceGitignoreContent()
        {
            var gitignoreContent = $@"
# Bravo workspace config file
{WorkspaceConfigFileName}
";
            return gitignoreContent;
        }
    }
}