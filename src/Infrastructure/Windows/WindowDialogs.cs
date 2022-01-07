using Bravo.Infrastructure.Windows.Interop;
using Sqlbi.Bravo.Infrastructure.Extensions;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Windows
{
    internal enum DialogResult
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

    /// <summary>
    /// .NET wrapper around the Win32 open file dialog
    /// </summary>
    internal class OpenFileDialog
    {
        /// <summary>
        /// Which file extension, allowed by the Filter is the default
        /// </summary>
        public string DefaultExt { get; set; } = null;
        
        /// <summary>
        /// Is filled with the full path to the selected file, if on is selected. Can be pre-filled with a default.
        /// </summary>
        public string File { get; set; } = null;
        
        /// <summary
        /// >Null separated list of descriptions and file extensions. E.g. "Log files\0*.log\0Batch files\0*.bat\0"
        /// </summary>
        public string Filter { get; set; } = null;

        public string InitialDirectory { get; set; } = null;

        /// <summary>
        /// Title for the dialog box
        /// </summary>
        public string Title { get; set; } = null;

        public DialogResult ShowDialog(IntPtr hWnd)
        {
            var ofn = new Comdlg32.OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            ofn.hwndOwner = hWnd;
            ofn.hInstance = IntPtr.Zero;
            ofn.lpstrTitle = Title.NullIfEmpty();
            ofn.lpstrDefExt = DefaultExt.NullIfEmpty();
            ofn.lpstrFilter = Filter.ToFileDialogFilterString();
            ofn.lpstrInitialDir = InitialDirectory.NullIfEmpty();
            ofn.lpstrFile = new string(new char[256]);
            ofn.nMaxFile = ofn.lpstrFile.Length;
            ofn.lpstrFileTitle = new string(new char[64]);
            ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;

            if (!Comdlg32.GetOpenFileName(ofn))
                return DialogResult.Cancel;

            File = ofn.lpstrFile;
            return DialogResult.OK;
        }
    }

    internal abstract class CharBuffer
    {
        public abstract int Length { get; }

        [SecurityCritical]
        internal static CharBuffer CreateBuffer(int size)
        {
            if (Marshal.SystemDefaultCharSize == 1)
                return new AnsiCharBuffer(size);

            return new UnicodeCharBuffer(size);
        }

        internal abstract IntPtr AllocCoTaskMem();

        internal abstract string GetString();

        internal abstract void PutCoTaskMem(IntPtr ptr);

        internal abstract void PutString(string s);
    }

    internal class AnsiCharBuffer : CharBuffer
    {
        private byte[] _buffer;
        private int _offset;

        public override int Length => _buffer.Length;

        internal AnsiCharBuffer(int size)
        {
            _buffer = new byte[size];
        }

        internal override IntPtr AllocCoTaskMem()
        {
            var ptr = Marshal.AllocCoTaskMem(_buffer.Length);
            Marshal.Copy(_buffer, 0, ptr, _buffer.Length);
            return ptr;
        }

        internal override string GetString()
        {
            int i;
            for (i = _offset; i < _buffer.Length && _buffer[i] != 0; i++)
            {
            }

            var @string = Encoding.Default.GetString(_buffer, _offset, i - _offset);
            if (i < _buffer.Length)
                i++;

            _offset = i;
            return @string;
        }

        internal override void PutCoTaskMem(IntPtr ptr)
        {
            Marshal.Copy(ptr, _buffer, 0, _buffer.Length);
            _offset = 0;
        }

        internal override void PutString(string s)
        {
            var bytes = Encoding.Default.GetBytes(s);
            var length = Math.Min(bytes.Length, _buffer.Length - _offset);
            
            Array.Copy(bytes, 0, _buffer, _offset, length);
            _offset += length;

            if (_offset < _buffer.Length)
                _buffer[_offset++] = 0;
        }
    }

    internal class UnicodeCharBuffer : CharBuffer
    {
        private char[] _buffer;
        private int _offset;

        public override int Length => _buffer.Length;

        internal UnicodeCharBuffer(int size)
        {
            _buffer = new char[size];
        }

        internal override IntPtr AllocCoTaskMem()
        {
            var ptr = Marshal.AllocCoTaskMem(_buffer.Length * 2);
            Marshal.Copy(_buffer, 0, ptr, _buffer.Length);
            return ptr;
        }

        internal override string GetString()
        {
            int i;
            for (i = _offset; i < _buffer.Length && _buffer[i] != 0; i++)
            {
            }

            var result = new string(_buffer, _offset, i - _offset);
            if (i < _buffer.Length)
                i++;

            _offset = i;
            return result;
        }

        internal override void PutCoTaskMem(IntPtr ptr)
        {
            Marshal.Copy(ptr, _buffer, 0, _buffer.Length);
            _offset = 0;
        }

        internal override void PutString(string value)
        {
            var count = Math.Min(value.Length, _buffer.Length - _offset);
            
            value.CopyTo(0, _buffer, _offset, count);
            _offset += count;

            if (_offset < _buffer.Length)
                _buffer[_offset++] = '\0';
        }
    }

    /// <summary>
    /// .NET wrapper around the Win32 save file dialog
    /// </summary>
    internal class SaveFileDialog
    {
        public string DefaultExt { get; set; } = null;

        public string File { get; set; } = null;

        public string Filter { get; set; } = null;

        public string InitialDirectory { get; set; } = null;

        public string Title { get; set; } = null;

        public DialogResult ShowDialog(IntPtr hWnd)
        {
            var ofn = new Comdlg32.OPENFILENAME_I();
            try
            {
                var charBuffer = CharBuffer.CreateBuffer(8192);

                ofn.lStructSize = Marshal.SizeOf(typeof(Comdlg32.OPENFILENAME_I));
                ofn.hwndOwner = hWnd;
                ofn.hInstance = IntPtr.Zero;
                ofn.lpstrFilter = Filter.ToFileDialogFilterString();
                ofn.lpstrFile = charBuffer.AllocCoTaskMem();
                ofn.nMaxFile = charBuffer.Length;
                ofn.lpstrInitialDir = InitialDirectory.NullIfEmpty();
                ofn.lpstrTitle = Title.NullIfEmpty();
                ofn.lpstrDefExt = DefaultExt.NullIfEmpty();

                if (!Comdlg32.GetSaveFileName(ofn))
                    return DialogResult.Cancel;
                
                charBuffer.PutCoTaskMem(ofn.lpstrFile);
                File = charBuffer.GetString();

                return DialogResult.OK;
            }
            finally
            {
                //charBuffer = null;
                if (ofn.lpstrFile != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(ofn.lpstrFile);
            }

            //var ofn = new Comdlg32.OPENFILENAME();
            //ofn.lStructSize = Marshal.SizeOf(ofn);
            //ofn.hwndOwner = hWnd;
            //ofn.hInstance = IntPtr.Zero;
            //ofn.lpstrTitle = Title.NullIfEmpty();
            //ofn.lpstrDefExt = DefaultExt.NullIfEmpty();
            //ofn.lpstrFilter = Filter.ToFileDialogFilterString();
            //ofn.lpstrInitialDir = InitialDirectory.NullIfEmpty();
            //ofn.lpstrFile = new string(new char[256]);
            //ofn.nMaxFile = ofn.lpstrFile.Length;
            //ofn.lpstrFileTitle = new string(new char[64]);
            //ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;

            //if (!Comdlg32.GetSaveFileName(ofn))
            //    return DialogResult.Cancel;

            //File = ofn.lpstrFile;
            //return DialogResult.OK;
        }
    }

    /// <summary>
    /// .NET wrapper around the Win32 color picker dialog
    /// </summary>
    internal class ColorPickerDialog
    {
        private readonly int[] _customColors = new int[16]
        {
            0x00FFFFFF, 0x00C0C0C0, 0x00808080, 0x00000000,
            0x00FF0000, 0x00800000, 0x00FFFF00, 0x00808000,
            0x0000FF00, 0x00008000, 0x0000FFFF, 0x00008080,
            0x000000FF, 0x00000080, 0x00FF00FF, 0x00800080,
        };

        public Color Color { get; set; }

        public DialogResult ShowDialog(IntPtr hWnd)
        {
            var cc = new Comdlg32.CHOOSECOLOR();
            cc.lStructSize = Marshal.SizeOf(cc);

            var lpCustColors = Marshal.AllocCoTaskMem(16 * sizeof(int));
            try
            {
                Marshal.Copy(_customColors, 0, lpCustColors, 16);
                cc.hwndOwner = hWnd;
                cc.hInstance = IntPtr.Zero;
                cc.lpCustColors = lpCustColors;
                cc.rgbResult = ColorTranslator.ToWin32(Color);
                cc.Flags = Comdlg32.CHOOSECOLORFLAGS.CC_RGBINIT;

                if (!Comdlg32.ChooseColor(cc))
                    return DialogResult.Cancel;

                if (cc.rgbResult != ColorTranslator.ToWin32(Color))
                    Color = ColorTranslator.FromOle(cc.rgbResult);
                
                Marshal.Copy(lpCustColors, _customColors, 0, 16);
                return DialogResult.OK;
            }
            finally
            {
                Marshal.FreeCoTaskMem(lpCustColors);
            }
        }
    }
}
