using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure
{
    internal static class AppConstants
    {
        private static readonly string EnvironmentSpecialFolderLocalApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly FileVersionInfo VersionInfo = FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule!.FileName!);

        public static readonly bool IsDebug = VersionInfo.IsDebug;
        public static readonly string ApplicationName = "Bravo";
        public static readonly string ApplicationStoreAliasName = "BravoStore";
        public static readonly string ApplicationMainWindowTitle = "Bravo for Power BI";
        public static readonly string ApplicationInstanceUniqueName = $"{ApplicationName}-{Guid.NewGuid():D}";
        public static readonly string ApplicationFolderLocalDataPath = Path.Combine(EnvironmentSpecialFolderLocalApplicationData, ApplicationName);
        public static readonly string ApplicationFolderTempDataPath = Path.Combine(ApplicationFolderLocalDataPath, ".temp");
        public static readonly bool TelemetryEnabledDefault = true;
        public static readonly string TelemetryInstrumentationKey = "47a8970c-6293-408a-9cce-5b7b311574d3";
        public static readonly string PBIDesktopProcessName = "PBIDesktop";
        public static readonly string PBIDesktopMainWindowTitleSuffix = " - Power BI Desktop";
        public static readonly string DefaultMsalTokenCacheFilePath = Path.Combine(ApplicationFolderLocalDataPath!, ".msalcache");
        public static readonly TimeSpan MSALSignInTimeout = TimeSpan.FromMinutes(5);

        static AppConstants()
        {
            ApplicationFileVersion = VersionInfo.FileVersion ?? throw new ConfigurationErrorsException(nameof(VersionInfo.FileVersion));
            //ApplicationHostWindowTitle = VersionInfo.ProductName ?? throw new ConfigurationErrorsException(nameof(VersionInfo.ProductName));
            DefaultJsonOptions = new(JsonSerializerDefaults.Web);
            DefaultJsonOptions.Converters.Add(new JsonStringEnumMemberConverter()); // https://github.com/dotnet/runtime/issues/31081#issuecomment-578459083
        }

        public static string ApplicationFileVersion { get; }

        public static JsonSerializerOptions DefaultJsonOptions { get; }
    }
}
