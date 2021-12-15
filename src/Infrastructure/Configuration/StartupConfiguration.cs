using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using Sqlbi.Infrastructure;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Sqlbi.Bravo.Infrastructure.Configuration
{
    public static class StartupConfiguration
    {
        public static void FromCommandLineArguments(AppStartupOptions options)
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

            options.IsExecutedAsExternalTool = result.HasOption(serverOption) || result.HasOption(databaseOption);
            options.CommandLineErrors = result.Errors.Select((e) => e.Message).ToList().AsReadOnly();

            if (options.CommandLineErrors.Count == 0)
            {
                options.ArgumentServerName = result.ValueForOption(serverOption);
                options.ArgumentDatabaseName = result.ValueForOption(databaseOption);
            }
        }

        public static void Configure()
        {
            ConfigureSecurityProtocols();
            ConfiguretCurrentDirectory();
            ConfigureProcessDPIAware();
        }

        private static void ConfiguretCurrentDirectory()
        {
#if !DEBUG
            var path = AppContext.BaseDirectory;
            System.IO.Directory.SetCurrentDirectory(path);
#endif
        }

        private static void ConfigureSecurityProtocols()
        {
            var includeTls = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

#pragma warning disable CS0618 // Type or member is obsolete - Justification is that we are removing SecurityProtocolType.Ssl3
            var excludeSsl = ServicePointManager.SecurityProtocol & ~SecurityProtocolType.Ssl3;
#pragma warning restore CS0618 // Type or member is obsolete

            ServicePointManager.SecurityProtocol = excludeSsl | includeTls;
        }

        private static void ConfigureProcessDPIAware()
        {
            NativeMethods.SetProcessDPIAware();
        }
    }
}
