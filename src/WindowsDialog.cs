using System;
using System.Drawing;
using System.Runtime.InteropServices;


namespace Sqlbi.Bravo.WindowsDialogs
{
    public class OpenFileDialog
    {
        ///<summary>Which file extension, allowed by the Filter is the default</summary>
        public string DefaultExtension { get; set; } = null;
        ///<summary>Is filled with the full path to the selected file, if on is selected. Can be pre-filled with a default.</summary>
        public string File { get; set; } = null;
        ///<summary>Null separated list of descriptions and file extensions. E.g. "Log files\0*.log\0Batch files\0*.bat\0"</summary>
        public string Filter { get; set; } = null;
        public string InitialDirectory { get; set; } = null;
        ///<summary>Title for the dialog box</summary>
        public string Title { get; set; } = null;

        public DialogResult ShowDialog()
        {
            var ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);

            if (string.IsNullOrWhiteSpace(Filter))
                ofn.filter = null;
            else
                ofn.filter = Filter;

            ofn.file = new string(new char[256]);
            ofn.maxFile = ofn.file.Length;

            ofn.fileTitle = new string(new char[64]);
            ofn.maxFileTitle = ofn.fileTitle.Length;

            if (string.IsNullOrWhiteSpace(InitialDirectory))
                ofn.initialDir = null;
            else
                ofn.initialDir = InitialDirectory;

            if (string.IsNullOrWhiteSpace(Title))
                ofn.title = null;
            else
                ofn.title = Title;

            if (string.IsNullOrWhiteSpace(DefaultExtension))
                ofn.defExt = null;
            else
                ofn.defExt = DefaultExtension;

            if (Win32Dialogs.GetOpenFileName(ofn))
            {
                File = ofn.file;
                return DialogResult.OK;
            }
            else
            {
                return DialogResult.Cancel;
            }
        }
    }

    //<summary>.NET wrapper around the Win32 open file dialog</summary>
    public class SaveFileDialog
    {
        ///<summary>Which file extension, allowed by the Filter is the default</summary>
        public string DefaultExtension { get; set; } = null;
        ///<summary>Is filled with the full path to the selected file, if on is selected. Can be pre-filled with a default.</summary>
        public string File { get; set; } = null;
        ///<summary>Null separated list of descriptions and file extensions. E.g. "Log files\0*.log\0Batch files\0*.bat\0"</summary>
        public string Filter { get; set; } = null;
        public string InitialDirectory { get; set; } = null;
        ///<summary>Title for the dialog box</summary>
        public string Title { get; set; } = null;

        public DialogResult ShowDialog()
        {
            var ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);

            if (string.IsNullOrWhiteSpace(Filter))
                ofn.filter = null;
            else
                ofn.filter = Filter;

            ofn.file = new String(new char[256]);
            ofn.maxFile = ofn.file.Length;

            ofn.fileTitle = new String(new char[64]);
            ofn.maxFileTitle = ofn.fileTitle.Length;

            if (string.IsNullOrWhiteSpace(InitialDirectory))
                ofn.initialDir = null;
            else
                ofn.initialDir = InitialDirectory;

            if (string.IsNullOrWhiteSpace(Title))
                ofn.title = null;
            else
                ofn.title = Title;

            if (string.IsNullOrWhiteSpace(DefaultExtension))
                ofn.defExt = null;
            else
                ofn.defExt = DefaultExtension;

            if (Win32Dialogs.GetSaveFileName(ofn))
            {
                File = ofn.file;
                return DialogResult.OK;
            }
            else
            {
                return DialogResult.Cancel;
            }
        }
    }

    //<summary>.NET wrapper around the Win32 open file dialog</summary>
    public class ColorPickerDialog
    {
        public Color Color { get; set; }

        public DialogResult ShowDialog()
        {
            var cc = new ChooseColor();
            cc.structSize = Marshal.SizeOf(cc);

            //Does not seem to work unless custom colors are initialized.
            var lpCustColors = Marshal.AllocCoTaskMem(16 * sizeof(int));
            cc.custColors = lpCustColors;
            cc.flags = ChooseColorFlags.RgbInit;
            try
            {
                Marshal.Copy(customColors, 0, lpCustColors, 16);

                if (Win32Dialogs.ChooseColor(cc))
                {
                    Color = ColorTranslator.FromWin32(cc.rgbResult);
                    return DialogResult.OK;
                }
                else
                {
                    return DialogResult.Cancel;
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(lpCustColors);
            }
        }


        private readonly int[] customColors = new int[16] {
            0x00FFFFFF, 0x00C0C0C0, 0x00808080, 0x00000000,
            0x00FF0000, 0x00800000, 0x00FFFF00, 0x00808000,
            0x0000FF00, 0x00008000, 0x0000FFFF, 0x00008080,
            0x000000FF, 0x00000080, 0x00FF00FF, 0x00800080,
        };
    }

    public enum DialogResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Abort = 3,
        Retry = 4,
        Ignore = 5,
        Yes = 6,
        No = 7,
    }
}
