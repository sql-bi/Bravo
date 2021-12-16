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
        [JsonPropertyName("executedAsExternalTool")]
        public bool IsExecutedAsExternalTool { get; set; }

        [JsonIgnore]
        public ReadOnlyCollection<string>? CommandLineErrors { get; set; }

        [JsonPropertyName("serverName")]
        public string? ArgumentServerName { get; set; }

        [JsonPropertyName("databaseName")]
        public string? ArgumentDatabaseName { get; set; }
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

            settings.IsExecutedAsExternalTool = result.HasOption(serverOption) || result.HasOption(databaseOption);
            settings.CommandLineErrors = result.Errors.Select((e) => e.Message).ToList().AsReadOnly();

            if (settings.CommandLineErrors.Count == 0)
            {
                settings.ArgumentServerName = result.ValueForOption(serverOption);
                settings.ArgumentDatabaseName = result.ValueForOption(databaseOption);
            }
        }
    }
}
