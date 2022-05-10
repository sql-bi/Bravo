#nullable disable

namespace Sqlbi.Bravo.Infrastructure.Windows
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    /// <summary>
    /// .NET wrapper around the Win32 open file dialog
    /// </summary>
    internal class OpenFileDialog
    {
        public string DefaultExt { get; set; } = null;
        
        public string File { get; set; } = null;
        
        public string Filter { get; set; } = null;

        public string InitialDirectory { get; set; } = null;

        public string Title { get; set; } = null;

        public DialogResult ShowDialog(IntPtr hWnd)
        {
            var ofn = new Comdlg32.OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            ofn.hwndOwner = hWnd;
            ofn.hInstance = IntPtr.Zero;
            ofn.lpstrTitle = Title.NullIfWhiteSpace();
            ofn.lpstrDefExt = DefaultExt.NullIfWhiteSpace();
            ofn.lpstrFilter = Filter.ToFileDialogFilterString();
            ofn.lpstrInitialDir = InitialDirectory.NullIfWhiteSpace();
            ofn.lpstrFile = new string(new char[Win32Constant.MAX_PATH]);
            ofn.nMaxFile = ofn.lpstrFile.Length;
            ofn.lpstrFileTitle = new string(new char[Win32Constant.MAX_PATH]);
            ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;

            if (!Comdlg32.GetOpenFileName(ofn))
                return DialogResult.Cancel;

            File = ofn.lpstrFile;
            return DialogResult.OK;
        }
    }

    /// <summary>
    /// .NET wrapper around the Win32 save file dialog
    /// </summary>
    internal class SaveFileDialog
    {
        public string DefaultExt { get; set; } = null;

        public string FileName { get; set; } = null;

        public string Filter { get; set; } = null;

        public string InitialDirectory { get; set; } = null;

        public string Title { get; set; } = null;

        public DialogResult ShowDialog(IntPtr hWnd)
        {
            var ofn = new Comdlg32.OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            ofn.hwndOwner = hWnd;
            ofn.hInstance = IntPtr.Zero;
            ofn.lpstrTitle = Title.NullIfWhiteSpace();
            ofn.lpstrDefExt = DefaultExt.NullIfWhiteSpace();
            ofn.lpstrFilter = Filter.ToFileDialogFilterString();
            ofn.lpstrInitialDir = InitialDirectory.NullIfWhiteSpace();
            ofn.lpstrFile = new string(new char[Win32Constant.MAX_PATH]);
            ofn.nMaxFile = ofn.lpstrFile.Length;
            ofn.lpstrFileTitle = new string(new char[Win32Constant.MAX_PATH]);
            ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;

            if (!Comdlg32.GetSaveFileName(ofn))
                return DialogResult.Cancel;

            FileName = ofn.lpstrFile;
            return DialogResult.OK;
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
            var lpCustColors = Marshal.AllocCoTaskMem(16 * sizeof(int));
            try
            {
                Marshal.Copy(_customColors, 0, lpCustColors, 16);

                var cc = new Comdlg32.CHOOSECOLOR();
                cc.lStructSize = Marshal.SizeOf(cc);
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

    internal class MessageDialog
    {
        public static void Show(string heading, string text)
        {
            var appIcon = Icon.ExtractAssociatedIcon(AppEnvironment.ProcessPath);
            var icon = new TaskDialogIcon(appIcon!);

            var page = new TaskDialogPage()
            {
                Caption = AppEnvironment.ApplicationMainWindowTitle,
                Heading = heading,
                Text = text,
                Icon = icon,
                AllowCancel = true
            };

            var hwndOwner = ProcessHelper.GetCurrentProcessMainWindowHandle();
            _ = TaskDialog.ShowDialog(hwndOwner, page, TaskDialogStartupLocation.CenterScreen);
        }

        public static TaskDialogButton ShowDialog(string heading, string text, string? footnoteText, bool allowCancel, params TaskDialogButton[] buttons)
        {
            var appIcon = Icon.ExtractAssociatedIcon(AppEnvironment.ProcessPath);
            var icon = new TaskDialogIcon(appIcon!);

            var page = new TaskDialogPage()
            {
                Caption = AppEnvironment.ApplicationMainWindowTitle,
                Heading = heading,
                Text = text,
                Icon = icon,
                AllowCancel = allowCancel, // || buttons.Any((button) => button == TaskDialogButton.Cancel),
                AllowMinimize = false
            };

            if (footnoteText is not null)
            {
                page.Footnote = new TaskDialogFootnote()
                {
                    Text = footnoteText,
                };
            }


            foreach (var button in buttons)
                page.Buttons.Add(button);

            var hwndOwner = ProcessHelper.GetCurrentProcessMainWindowHandle();
            var clickedButton = TaskDialog.ShowDialog(hwndOwner, page, TaskDialogStartupLocation.CenterScreen);

            return clickedButton;
        }
    }
}
