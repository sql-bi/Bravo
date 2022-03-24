namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    internal static class DesktopBridgeHelper
    {
        /// <summary>
        /// The process has no package identity
        /// </summary>
        private const long APPMODEL_ERROR_NO_PACKAGE = 15700L;

        private static bool IsWindows7OrLower()
        {
            var major = (double)Environment.OSVersion.Version.Major;
            var minor = (double)Environment.OSVersion.Version.Minor;

            var result = (major + minor / 10.0) <= 6.1;
            return result;
        }

        /// <summary>
        /// Returns true if the app is running as an MSIX package on Windows 10, version 1709 (build 16299) or later
        /// </summary>
        public static bool IsRunningAsMsixPackage()
        {
            // https://docs.microsoft.com/en-us/windows/msix/detect-package-identity

            if (IsWindows7OrLower()) 
                return false;

            var packageFullNameLength = 0;
            var packageFullName = new StringBuilder(0);
            _ = Kernel32.GetCurrentPackageFullName(ref packageFullNameLength, packageFullName);

            packageFullName = new StringBuilder(packageFullNameLength);
            var retval = Kernel32.GetCurrentPackageFullName(ref packageFullNameLength, packageFullName);

            return retval != APPMODEL_ERROR_NO_PACKAGE;
        }

        /// <summary>
        /// Attempt to start a new instance of the current application as administrator triggering the request for elevation via UAC
        /// </summary>
        /// <exception cref="InvalidOperationException">If the current process is not running as an MSIX package application</exception>
        public static bool AppRunAs(bool environmentExit, params string[] customArgs)
        {
            if (!IsRunningAsMsixPackage())
                throw new InvalidOperationException("The current process is not running as packaged application");

            // This requires AppExecutionAlias activation in the MSIX installation package
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
            var fileName = Path.Combine(localAppData, @"Microsoft\WindowsApps", Path.ChangeExtension(AppEnvironment.ApplicationStoreAliasName, ".exe"));

            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = fileName,
                Verb = "runas", // this will trigger the request for elevation via UAC
            };

            //foreach (var arg in Environment.GetCommandLineArgs().Skip(1))
            //    startInfo.ArgumentList.Add(arg);

            foreach (var arg in customArgs)
                startInfo.ArgumentList.Add(arg);

            var process = Process.Start(startInfo);
            if (process is null || process.HasExited)
                return false;

            if (environmentExit)
            {
                // Exit terminates the application immediately even if other threads are running
                // If called from a try or catch block, the code in any finally block does not execute
                Environment.Exit(0);
            }

            return true;
        }
    }
}
