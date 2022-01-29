using Sqlbi.Bravo.Infrastructure.Messages;
using Sqlbi.Bravo.Infrastructure.Windows;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Sqlbi.Bravo.Infrastructure
{
    internal class AppWindowSubclass : WindowSubclass
    {
        private readonly PhotinoNET.PhotinoWindow _window;
        private readonly IntPtr MSG_HANDLED = new(1);

        /// <summary>
        /// Try to install a WndProc subclass callback to hook messages sent to the selected <see cref="PhotinoNET.PhotinoWindow"/> window
        /// </summary>
        public static AppWindowSubclass Hook(PhotinoNET.PhotinoWindow window) => new(window);

        private AppWindowSubclass(PhotinoNET.PhotinoWindow window)
            : base(hWnd: window.WindowHandle)
        {
            _window = window;
        }

        protected override IntPtr WndProcHooked(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data)
        {
            switch (uMsg)
            {
                case (uint)WindowMessage.WM_COPYDATA:
                    {
                        HandleMsgWmCopyData(hWnd, copydataPtr: lParam);
                        return MSG_HANDLED;
                    }
                default:
                    break;
            }

            return base.WndProcHooked(hWnd, uMsg, wParam, lParam, id, data);
        }

        private void HandleMsgWmCopyData(IntPtr hWnd, IntPtr copydataPtr)
        {
            try
            {
                // Restore original size and position only if the window is minimized, otherwise keep current position
                if (_window.Minimized)
                    User32.ShowWindow(hWnd, User32.SW_RESTORE);

                // Regardless of the current state of the window, bring it to the foreground and activate it
                User32.SetForegroundWindow(hWnd);

                var copyDataObject = Marshal.PtrToStructure(copydataPtr, typeof(User32.COPYDATASTRUCT));
                if (copyDataObject == null)
                    return;

                var copyData = (User32.COPYDATASTRUCT)copyDataObject;
                if (copyData.cbData == 0)
                    return;

                var startupMessage = JsonSerializer.Deserialize<AppInstanceStartupMessage>(json: copyData.lpData);
                if (startupMessage is not null)
                {
#if DEBUG
                    var startupMessageString = JsonSerializer.Serialize(startupMessage, new JsonSerializerOptions { WriteIndented = true });
                    Trace.WriteLine($"::Bravo:INF:WndProcHook[WM_COPYDATA]:{ startupMessageString }");
#endif
                    var webMessageString = startupMessage.ToWebMessageString();
                    _window.SendWebMessage(webMessageString);
                }
            }
            catch (Exception ex)
            {
                var exceptionWebMessage = UnknownWebMessage.CreateFrom(ex);
                var exceptionWebMessageString = exceptionWebMessage.AsString;

                _window.SendWebMessage(exceptionWebMessageString);
            }
        }
    }
}
