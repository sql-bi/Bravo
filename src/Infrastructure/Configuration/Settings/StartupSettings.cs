using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Extensions;
using System;
using System.Collections.ObjectModel;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sqlbi.Infrastructure.Configuration.Settings
{
    public class StartupSettings
    {
        [JsonPropertyName("externalTool")]
        public bool IsExternalTool { get; set; }

        [JsonPropertyName("serverName")]
        public string? ArgumentServerName { get; set; }

        [JsonPropertyName("databaseName")]
        public string? ArgumentDatabaseName { get; set; }

        [JsonIgnore]
        public bool IsPBIDesktopExternalTool => IsExternalTool && AppConstants.PBIDesktopProcessName.Equals(ParentProcessName);

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

        public static StartupSettings Get()
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
            var parentProcess = Process.GetCurrentProcess().GetParent();

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

            var command = new RootCommand
            {
                serverOption,
                databaseOption
            };

            var args = Environment.GetCommandLineArgs();
            var result = command.Parse(args);

            settings.IsExternalTool = result.HasOption(serverOption) || result.HasOption(databaseOption);
            settings.CommandLineErrors = result.Errors.Select((e) => e.Message).ToList().AsReadOnly();

            if (settings.CommandLineErrors.Count == 0)
            {
                settings.ArgumentServerName = result.ValueForOption(serverOption);
                settings.ArgumentDatabaseName = result.ValueForOption(databaseOption);
            }

            settings.ParentProcessId = parentProcess.Id;
            settings.ParentProcessName = parentProcess.ProcessName;
            //settings.ParentProcessMainWindowHandle = parentProcess.MainWindowHandle;
            settings.ParentProcessMainWindowTitle = parentProcess.GetMainWindowTitle(settings.IsPBIDesktopExternalTool ? (windowTitle) => windowTitle.IsPBIDesktopMainWindowTitle() : default).ToPBIDesktopReportName();
        }
    }
}
