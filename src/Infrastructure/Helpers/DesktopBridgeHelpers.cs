using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class DesktopBridgeHelpers
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder packageFullName);

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
        /// This method returns true if the app is running as an MSIX package on Windows 10, version 1709 (build 16299) or later
        /// </summary>
        public static bool IsRunningAsUwp()
        {
            // https://docs.microsoft.com/en-us/windows/msix/detect-package-identity

            if (IsWindows7OrLower())
                return false;

            var packageFullNameLength = 0;
            var packageFullName = new StringBuilder(0);

            var retval = GetCurrentPackageFullName(ref packageFullNameLength, packageFullName);

            packageFullName = new StringBuilder(packageFullNameLength);
            retval = GetCurrentPackageFullName(ref packageFullNameLength, packageFullName);

            return retval != APPMODEL_ERROR_NO_PACKAGE;
        }
    }
}
