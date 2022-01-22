using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    ///***************************************************************************
    ///***************************************************************************
    /// <remarks>
    ///     This class uses undocumented APIs introduced in Windows 10 build 1809
    ///     APIs are not documented and exported by ordinal only.
    /// </remarks>
    ///***************************************************************************
    ///***************************************************************************
    internal static class Uxtheme
    {
        private enum PreferredAppMode
        {
            Default = 0,
            AllowDark = 1,
            ForceDark = 2,
            ForceLight = 3,
            Max = 4
        };

        /*
         * OS version 1809 ( OS build 17763 )
         */

        //[DllImport(ExternDll.Uxtheme, EntryPoint = "#104")]
        //private static extern void RefreshImmersiveColorPolicyState();

        [DllImport(ExternDll.Uxtheme, EntryPoint = "#132")]
        private static extern bool ShouldAppsUseDarkMode();

        //[DllImport(ExternDll.Uxtheme, EntryPoint = "#133")]
        //private static extern bool AllowDarkModeForWindow(IntPtr hWnd, bool allow);

        [DllImport(ExternDll.Uxtheme, EntryPoint = "#135")]
        private static extern bool AllowDarkModeForApp(bool allow); // !!! 135 !!! same ordinal in 1903 and 1809 

        //[DllImport(ExternDll.Uxtheme, EntryPoint = "#137")]
        //private static extern bool IsDarkModeAllowedForWindow(IntPtr hWnd);

        /*
         * OS version 1903 ( OS build 18362 )
         */

        [DllImport(ExternDll.Uxtheme, EntryPoint = "#135")]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern PreferredAppMode SetPreferredAppMode(PreferredAppMode mode); // !!! 135 !!! same ordinal in 1903 and 1809 

        [DllImport(ExternDll.Uxtheme, EntryPoint = "#138")]
        private static extern bool ShouldSystemUseDarkMode();

        //[DllImport(ExternDll.Uxtheme, EntryPoint = "#139")]
        //private static extern bool IsDarkModeAllowedForApp(IntPtr hWnd);

        /* */

        private static readonly bool IsWindows10OSVersion1809OrLower = Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build < 18362;

        private static bool IsDarkModeSupported()
        {
            // Windows 10 release information https://docs.microsoft.com/en-us/windows/release-health/release-information

            var version = Environment.OSVersion.Version;

            if (version.Major == 10 && version.Build >= 17763 /* Version 1809 */)
                return true;

            if (version.Major > 10)
                return true;

            return false;
        }

        private static void SetAppTheme(bool useDark)
        {
            if (IsWindows10OSVersion1809OrLower)
            {
                // Ordinal entry point *** 135 *** in 1809 (OS build 17763)
                _ = AllowDarkModeForApp(allow: useDark);
            }
            else
            {
                // Ordinal entry point *** 135 *** in 1903 (OS build 18362)
                _ = SetPreferredAppMode(useDark ? PreferredAppMode.ForceDark : PreferredAppMode.ForceLight);
            }
        }

        public static void SetStartupTheme(bool useDark)
        {
            if (IsDarkModeSupported())
            {
                // No need to send any messges here (i.e. WM_THEMECHANGED)
                SetAppTheme(useDark);
            }
        }

        public static void ChangeTheme(IntPtr hWnd, bool useDark)
        {
            if (IsDarkModeSupported())
            {
                SetAppTheme(useDark);
                User32.SendMessage(hWnd, WindowMessage.WM_THEMECHANGED, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public static bool IsSystemUsingDarkMode()
        {
            if (IsDarkModeSupported() && !SystemInformation.HighContrast)
            {
                if (IsWindows10OSVersion1809OrLower)
                {
                    return ShouldAppsUseDarkMode();
                }
                else
                {
                    return ShouldSystemUseDarkMode();
                }
            }

            return false;
        }
    }
}
