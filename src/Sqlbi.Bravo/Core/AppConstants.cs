using Serilog.Events;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo.Core
{
    internal static class AppConstants
    {
        private static readonly string EnvironmentSpecialFolderLocalApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        private static readonly string EnvironmentSpecialFolderCommonProgramFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);

        private static readonly FileVersionInfo VersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

        public static string ApplicationName { get; } = "SqlbiBravo";

        public static string ApplicationNameLabel { get; } = "Sqlbi Bravo";

        public static string ApplicationInstanceUniqueName { get; } = $"{ ApplicationName }-{Guid.NewGuid():D}";

        public static string ApplicationFolderLocalDataPath { get; } = Path.Combine(EnvironmentSpecialFolderLocalApplicationData, ApplicationName);

        public static bool ApplicationSettingsDefaultTelemetryEnabled { get; } = false;

        public static LogEventLevel ApplicationSettingsDefaultTelemetryLevel { get; } = LogEventLevel.Information;

        public static bool ApplicationSettingsDefaultProxyUseSystem { get; } = true;

        public static bool ApplicationSettingsDefaultUIShellBringToForegroundOnParentProcessMainWindowScreen { get; } = false;

        public static string UserSettingsFilePath { get; } = Path.Combine(ApplicationFolderLocalDataPath, "usersettings.json");

        public static string ApplicationProductVersion { get; } = VersionInfo.ProductVersion;

        public static Version ApplicationProductVersionNumber { get; } = new Version(VersionInfo.FileVersion);

        public static string[] CommandLineArgumentServerNameAliases { get; } = new string[] { "--server", "--s" };

        public static string[] CommandLineArgumentDatabaseNameAliases { get; } = new string[] { "--database", "--d" };

        public static string ProfileOptimizationProfileRootPath { get; } = Path.Combine(ApplicationFolderLocalDataPath, $"Startup{ RuntimeInformation.ProcessArchitecture }");

        public static string ProfileOptimizationProfileName { get; } = $"startup{ RuntimeInformation.ProcessArchitecture }.profile";

        public static string PowerBIDesktopProcessName { get; } = "PBIDesktop";

        public static string PowerBIDesktopExternalToolsDirectory { get; } = Path.Combine(EnvironmentSpecialFolderCommonProgramFilesX86, @"Microsoft Shared\Power BI Desktop\External Tools");

        public static Uri DaxFormatterTextFormatUri { get; } = new Uri("https://www.daxformatter.com/api/daxformatter/daxtextformat");

        public static TimeSpan DaxFormatterTextFormatTimeout { get; } = TimeSpan.FromSeconds(10);

        public static int DaxFormatterTextFormatRequestBatchMaxTextLength { get; } = 10000;

        public static string DaxFormatterTextFormatRequestBatchSeparator { get; } = "\r\n*^*\r\n";

        public static TimeSpan AnalysisServicesEventWatcherServiceConnectionStateWaitDelay { get; } = TimeSpan.FromSeconds(15);

        public static string TelemetrySettingsSectionName { get; } = "Telemetry";

        public static string TelemetryInstrumentationKey { get; } = "47a8970c-6293-408a-9cce-5b7b311574d3";

        static AppConstants()
        {
        }
    }
}
