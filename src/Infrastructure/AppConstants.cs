using System;
using System.Configuration;
using System.Diagnostics;

namespace Sqlbi.Bravo.Infrastructure
{
    internal static class AppConstants
    {
        public static string ApplicationName { get; } = "Sqlbi Bravo";

        public static string ApplicationFileVersion { get; }

        public static string ApplicationHostWindowTitle { get; } = "Bravo for Power BI";

        public static string ApplicationInstanceUniqueName { get; } = $"SqlbiBravo-{Guid.NewGuid():D}";

        public static string PBIDesktopProcessName { get; } = "PBIDesktop";

        static AppConstants()
        {
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule!.FileName!);

            ApplicationFileVersion = fileVersionInfo.FileVersion ?? throw new ConfigurationErrorsException(nameof(fileVersionInfo.FileVersion));
            //ApplicationHostWindowTitle = fileVersionInfo.ProductName ?? throw new ConfigurationErrorsException(nameof(fileVersionInfo.ProductName));
        }
    }
}
