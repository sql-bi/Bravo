using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using System;
using System.Runtime.InteropServices;

#nullable disable

namespace Bravo.Infrastructure.Windows.Interop
{
    internal static class Comdlg32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OPENFILENAME
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public string lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int flagsEx;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class CHOOSECOLOR
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public int rgbResult;
            public IntPtr lpCustColors;
            public int Flags;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
        }

        public static class CHOOSECOLORFLAGS
        {
            public const int CC_RGBINIT = 0x00000001;
            public const int CC_FULLOPEN = 0x00000002;
            public const int CC_PREVENTFULLOPEN = 0x00000004;
            public const int CC_SHOWHELP = 0x00000008;
            public const int CC_ENABLEHOOK = 0x00000010;
            public const int CC_ENABLETEMPLATE = 0x00000020;
            public const int CC_ENABLETEMPLATEHANDLE = 0x00000040;
            public const int CC_SOLIDCOLOR = 0x00000080;
            public const int CC_ANYCOLOR = 0x00000100;
        }

        [DllImport(ExternDll.Comdlg32, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] OPENFILENAME ofn);

        [DllImport(ExternDll.Comdlg32, CharSet = CharSet.Auto)]
        public static extern bool GetSaveFileName([In, Out] OPENFILENAME ofn);

        [DllImport(ExternDll.Comdlg32, CharSet = CharSet.Auto)]
        public static extern bool ChooseColor([In, Out] CHOOSECOLOR cc);
    }
}
