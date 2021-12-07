using System;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo.Infrastructure.Windows
{
    internal static class Win32Dialogs
    {
        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetSaveFileName([In, Out] OpenFileName ofn);

        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto)]
        public static extern bool ChooseColor([In, Out] ChooseColor cc);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        //static void Main()
        //{
        //    MessageBox(new IntPtr(0), "Hello World!", "Hello Dialog", 0);
        //}
    }

    //DATA STRUCTURES
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenFileName
    {
        public int structSize = 0;                  //DWORD         lStructSize
        public IntPtr dlgOwner = IntPtr.Zero;       //HWND          hwndOwner
        public IntPtr instance = IntPtr.Zero;       //HINSTANCE     hInstance
        public string filter = null;                //LPCTSTR       lpstrFilter
        public string customFilter = null;          //LPTSTR        lpstrCustomFilter
        public int maxCustFilter = 0;               //DWORD         nMaxCustFilter
        public int filterIndex = 0;                 //DWORD         nFilterIndex
        public string file = null;                  //LPTSTR        lpstrFile
        public int maxFile = 0;                     //DWORD         nMaxFile
        public string fileTitle = null;             //LPTSTR        lpstrFileTitle
        public int maxFileTitle = 0;                //DWORD         nMaxFileTitle
        public string initialDir = null;            //LPCTSTR       lpstrInitialDir
        public string title = null;                 //LPCTSTR       lpstrTitle
        public int flags = 0;                       //DWORD         Flags
        public short fileOffset = 0;                //WORD          nFileOffset
        public short fileExtension = 0;             //WORD          nFileExtension
        public string defExt = null;                //LPCSTR        lpstrDefExt
        public IntPtr custData = IntPtr.Zero;       //LPARAM        lCustData
        public IntPtr hook = IntPtr.Zero;           //LPOFNHOOKPROC lpfnHook
        public string templateName = null;          //LPCTSTR       lpTemplateName
        public IntPtr reservedPtr = IntPtr.Zero;    //void *        pvReserved
        public int reservedInt = 0;                 //DWORD         dwReserved
        public int flagsEx = 0;                     //DWORD         FlagsEx
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class ChooseColor
    {
        public int structSize = 0;                  //DWORD         lStructSize
        public IntPtr dlgOwner = IntPtr.Zero;       //HWND          hwndOwner
        public IntPtr instance = IntPtr.Zero;       //HWND          hInstance
        public int rgbResult = 0;                   //COLORREF      rgbResult
        public IntPtr custColors = IntPtr.Zero;     //COLORREF *    lpCustColors
        public int flags = 0;                       //DWORD         Flags
        public IntPtr custData = IntPtr.Zero;       //LPARAM        lCustData
        public IntPtr hook = IntPtr.Zero;           //LPCCHOOKPROC  lpfnHook
        public IntPtr templateName = IntPtr.Zero;   //LPCSTR        lpTemlateName
        //public IntPtr editInfo = IntPtr.Zero;     //LPEDITMENU    lpEditInfo
    }
    delegate IntPtr CCHookProc(IntPtr hWnd, ushort msg, int wParam, int lParam);

    public static class ChooseColorFlags
    {
        public const int RgbInit = 0x00000001;
        public const int FullOpen = 0x00000002;
        public const int PreventFullOpen = 0x00000004;
        public const int ShowHelp = 0x00000008;
        public const int EnableHook = 0x00000010;
        public const int EnableTemplate = 0x00000020;
        public const int EnableTemplateHandle = 0x00000040;
        public const int SolidColor = 0x00000080;
        public const int AnyColor = 0x00000100;
    }
}
