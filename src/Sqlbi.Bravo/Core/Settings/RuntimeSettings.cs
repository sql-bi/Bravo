using Sqlbi.Bravo.Core.Management;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;

namespace Sqlbi.Bravo.Core.Settings
{
    internal class RuntimeSettings : IRuntimeSettings
    {
        public RuntimeSettings()
        {
            var parentProcess = System.Diagnostics.Process.GetCurrentProcess().GetParent();
            ParentProcessId = parentProcess.Id;
            ParentProcessName = parentProcess.ProcessName;
            ParentProcessMainWindowTitle = parentProcess.MainWindowTitle;
            ParentProcessMainWindowHandle = parentProcess.MainWindowHandle;

            ParseCommandLineArgs();
        }

        public string ServerName { get; private set; }

        public string DatabaseName { get; private set; }

        public int ParentProcessId { get; private set; }

        public string ParentProcessName { get; private set; }

        public string ParentProcessMainWindowTitle { get; private set; }

        public IntPtr ParentProcessMainWindowHandle { get; private set; }

        public bool IsExecutedAsExternalTool { get; private set; }

        public bool IsExecutedAsExternalToolForPowerBIDesktop => IsExecutedAsExternalTool && AppConstants.PowerBIDesktopProcessName.Equals(ParentProcessName);

        public string ExternalToolInstanceId => $"{ AppConstants.ApplicationName }|{ ServerName }|{ DatabaseName }";

        public bool HasCommandLineParseErrors => CommandLineParseErrors.Any();

        public IReadOnlyCollection<string> CommandLineParseErrors { get; private set; }

        private void ParseCommandLineArgs()
        {
            var serverNameOption = new Option<string>(AppConstants.CommandLineArgumentServerNameAliases)
            {  
                Description = "Server name", 
                IsRequired = true 
            };
            var databaseNameOption = new Option<string>(AppConstants.CommandLineArgumentDatabaseNameAliases)
            { 
                Description = "Database name", 
                IsRequired = true 
            };
            var command = new RootCommand
            {
                serverNameOption,
                databaseNameOption
            };

            var args = Environment.GetCommandLineArgs();
            var result = command.Parse(args);

            IsExecutedAsExternalTool = result.HasOption(serverNameOption) || result.HasOption(databaseNameOption);

            var messages = result.Errors.Select((e) => e.Message).ToArray();
            CommandLineParseErrors = new ReadOnlyCollection<string>(messages);

            if (CommandLineParseErrors.Count == 0)
            {
                ServerName = result.ValueForOption(serverNameOption);
                DatabaseName = result.ValueForOption(databaseNameOption);
            }
        }
    }
}
