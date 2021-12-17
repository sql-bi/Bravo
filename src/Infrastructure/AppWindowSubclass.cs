using Sqlbi.Bravo.Infrastructure.Messages;
using Sqlbi.Bravo.Infrastructure.Windows;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using System;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Sqlbi.Bravo.Infrastructure
{
    internal class AppWindowSubclass : WindowSubclass
    {
        private readonly PhotinoNET.PhotinoWindow _window;
        private readonly IntPtr MSG_HANDLED = new(1);

        public AppWindowSubclass(PhotinoNET.PhotinoWindow window)
            : base(hWnd: window.WindowHandle)
        {
            _window = window;
        }

        protected override IntPtr WndProcHooked(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data)
        {
            switch (uMsg)
            {
                case NativeMethods.WM_COPYDATA:
                    {
                        // Restore original size and position only if the window is minimized, otherwise keep current position
                        if (_window.Minimized) NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                        // Regardless of current status, bring to front and activate the window
                        NativeMethods.SetForegroundWindow(hWnd);

                        try
                        {
                            HandleMsgWmCopyData(copydataPtr: lParam);
                        }
                        catch
                        {
                            // Here we ignore any exception to avoid possible payload deserialization vulnerabilities
                        }

                        return MSG_HANDLED;
                    }
                default:
                    break;
            }

            return base.WndProcHooked(hWnd, uMsg, wParam, lParam, id, data);
        }

        private void HandleMsgWmCopyData(IntPtr copydataPtr)
        {
            var copyDataObject = Marshal.PtrToStructure(copydataPtr, typeof(NativeMethods.COPYDATASTRUCT));
            if (copyDataObject == null)
                return;

            var copyData = (NativeMethods.COPYDATASTRUCT)copyDataObject;
            if (copyData.cbData == 0)
                return;

            var message = JsonSerializer.Deserialize<AppInstanceStartedMessage>(json: copyData.lpData);
            if (message?.IsExternalTool == true)
            {
                // TODO: here we are ignoring non-externaltool invocations
                // TODO: (WIP)
                var messageString = JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true });
                System.Diagnostics.Trace.WriteLine($"::Bravo:INF:WndProcHook[WM_COPYDATA]:{ messageString }");
                _window.OpenAlertWindow("::Bravo:INF:WndProcHook[WM_COPYDATA]", messageString);
                _window.SendWebMessage(messageString);
            }
        }
    }
}
