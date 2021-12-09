using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace Sqlbi.Bravo.Infrastructure
{
    internal static class AppConstants
    {
        private static readonly string EnvironmentSpecialFolderLocalApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly FileVersionInfo VersionInfo = FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule!.FileName!);

        public static readonly string ApplicationName = "SqlbiBravo";
        public static readonly string ApplicationHostWindowTitle = "Bravo for Power BI";
        public static readonly string ApplicationInstanceUniqueName = $"{ApplicationName}-{Guid.NewGuid():D}";
        public static readonly string ApplicationFolderLocalDataPath = Path.Combine(EnvironmentSpecialFolderLocalApplicationData, ApplicationName);
        public static readonly string PBIDesktopProcessName = "PBIDesktop";
        public static readonly string DefaultTokenCacheFilePath = Path.Combine(ApplicationFolderLocalDataPath!, ".msalcache.bin");

        static AppConstants()
        {
            ApplicationFileVersion = VersionInfo.FileVersion ?? throw new ConfigurationErrorsException(nameof(VersionInfo.FileVersion));
            //ApplicationHostWindowTitle = VersionInfo.ProductName ?? throw new ConfigurationErrorsException(nameof(VersionInfo.ProductName));
        }

        public static string ApplicationFileVersion { get; }
    }
}
