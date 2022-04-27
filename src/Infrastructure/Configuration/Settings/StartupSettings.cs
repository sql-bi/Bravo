namespace Sqlbi.Bravo.Infrastructure.Configuration.Settings
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using System;
    using System.Collections.ObjectModel;
    using System.CommandLine;
    using System.CommandLine.Parsing;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json.Serialization;

    internal class StartupSettings
    {
        [JsonPropertyName("isEmpty")]
        public bool IsEmpty { get; set; }

        [JsonPropertyName("externalTool")]
        public bool IsExternalTool { get; set; }

        [JsonPropertyName("serverName")]
        public string? ArgumentServerName { get; set; }

        [JsonPropertyName("databaseName")]
        public string? ArgumentDatabaseName { get; set; }

        [JsonIgnore]
        public bool IsPBIDesktopExternalTool => IsExternalTool && AppEnvironment.PBIDesktopProcessName.Equals(ParentProcessName, StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public int? ParentProcessId { get; set; }

        [JsonIgnore]
        public string? ParentProcessName { get; set; }

        //[JsonIgnore]
        //public IntPtr ParentProcessMainWindowHandle { get; set; }

        [JsonIgnore]
        public string? ParentProcessMainWindowTitle { get; set; }

        [JsonIgnore]
        public ReadOnlyCollection<string>? CommandLineErrors { get; set; }

        public static StartupSettings CreateFromCommandLineArguments()
        {
            var settings = new StartupSettings();
            {
                settings.FromCommandLineArguments();
            }

            return settings;
        }
    }

    internal static class StartupSettingsExtensions
    {
        public static void FromCommandLineArguments(this StartupSettings settings)
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 1)
            {
                // No args provided
                settings.IsEmpty = true;
                return;
            }

            var serverOption = new Option<string>(new[] { "--server", "--s" })
            {
                Description = "Server name",
                IsRequired = true
            };

            var databaseOption = new Option<string>(new[] { "--database", "--d" })
            {
                Description = "Database name",
                IsRequired = true
            };

            var parentProcessIdOption = new Option<string>(new[] { "--ppid" })
            {
                Description = "Parent process ID",
                IsRequired = false
            };

            var command = new RootCommand
            {
                serverOption,
                databaseOption,
                parentProcessIdOption
            };

            var parseResult = command.Parse(args);

            settings.IsExternalTool = parseResult.HasOption(serverOption) || parseResult.HasOption(databaseOption);
            settings.CommandLineErrors = parseResult.Errors.Select((e) => e.Message).ToList().AsReadOnly();

            if (settings.CommandLineErrors.Count == 0)
            {
                settings.ArgumentServerName = parseResult.ValueForOption(serverOption);
                settings.ArgumentDatabaseName = parseResult.ValueForOption(databaseOption);
            }

            Process? parentProcess = null;
            {
                if (AppEnvironment.DeploymentMode == AppDeploymentMode.Packaged && parseResult.HasOption(parentProcessIdOption))
                {
                    var parentProcessId = parseResult.ValueForOption<int>(parentProcessIdOption);
                    parentProcess = ProcessHelper.SafeGetProcessById(parentProcessId);
                }
                else
                {
                    parentProcess = ProcessHelper.GetParentProcess();
                }

                if (parentProcess is not null)
                {
                    settings.ParentProcessId = parentProcess.Id;
                    settings.ParentProcessName = parentProcess.ProcessName;
                    //settings.ParentProcessMainWindowHandle = parentProcess.MainWindowHandle;
                    settings.ParentProcessMainWindowTitle = settings.IsPBIDesktopExternalTool ? parentProcess.GetPBIDesktopMainWindowTitle() : parentProcess.GetMainWindowTitle();
                }
            }
            parentProcess?.Dispose();
        }
    }
}
