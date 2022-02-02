namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    using System;
    using System.Runtime.InteropServices;

    ///***************************************************************************
    ///***************************************************************************
    /// <remarks>
    ///     This class uses undocumented APIs introduced in Windows 10 build 1809
    ///     APIs are not documented and exported by ordinal only.
    ///
    ///     Discussion: Dark mode for applications
    ///     https://github.com/microsoft/WindowsAppSDK/issues/41#issue-622186032
    /// </remarks>
    ///***************************************************************************
    ///***************************************************************************
    internal static class Uxtheme
    {
        public enum PreferredAppMode
        {
            Default = 0,
            AllowDark = 1,
            ForceDark = 2,
            ForceLight = 3,
            Max = 4
        };

        /*
         * The Boolean representation that is required by the unmanaged method should be determined and matched to the appropriate System.Runtime.InteropServices.UnmanagedType.
         * UnmanagedType.Bool is the Win32 BOOL type, which is always 4 bytes. UnmanagedType.U1 should be used for C++ bool or other 1-byte types
         * https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1414?view=vs-2022#rule-description
         */

        /*
         * OS version 1809 ( OS build 17763 )
         */

        /// <summary>
        /// This undocumented API apparently triggers a refresh/repaint after changing the dark mode of a window
        /// </summary>
        [DllImport(ExternDll.Uxtheme, EntryPoint = "#104")]
        public static extern void RefreshImmersiveColorPolicyState();

        [DllImport(ExternDll.Uxtheme, EntryPoint = "#132")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool ShouldAppsUseDarkMode();

        [DllImport(ExternDll.Uxtheme, EntryPoint = "#133")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool AllowDarkModeForWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.U1)] bool allow);

        [DllImport(ExternDll.Uxtheme, EntryPoint = "#135")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool AllowDarkModeForApp([MarshalAs(UnmanagedType.U1)] bool allow); // !!! 135 !!! same ordinal in 1903 and 1809 

        [DllImport(ExternDll.Uxtheme, EntryPoint = "#137")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool IsDarkModeAllowedForWindow(IntPtr hWnd);

        /*
         * OS version 1903 ( OS build 18362 )
         */

        [DllImport(ExternDll.Uxtheme, EntryPoint = "#135")]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern PreferredAppMode SetPreferredAppMode(PreferredAppMode mode); // !!! 135 !!! same ordinal in 1903 and 1809 

        //[DllImport(ExternDll.Uxtheme, EntryPoint = "#138")]
        //[return: MarshalAs(UnmanagedType.U1)]
        //public static extern bool ShouldSystemUseDarkMode();

        [DllImport(ExternDll.Uxtheme, EntryPoint = "#139")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool IsDarkModeAllowedForApp(IntPtr hWnd);
    }
}
