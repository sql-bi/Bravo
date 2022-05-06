namespace Sqlbi.Bravo.Infrastructure
{
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Windows;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System;

    internal class AppWindowSubclass : WindowSubclass
    {
        private readonly IntPtr TRUE = new(1); // Message handled, do not call the next handler in the subclass chain

        /// <summary>
        /// Try to install a WndProc subclass callback to hook messages sent to the selected window
        /// </summary>
        public static AppWindowSubclass Hook(IntPtr hWnd) => new(hWnd);

        private AppWindowSubclass(IntPtr hWnd)
            : base(hWnd)
        {
        }

        protected override IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
        {
            switch (uMsg)
            {
                //case (uint)WindowMessage.WM_COPYDATA:
                //    {
                //        if (HandleMsgWmCopyData(hWnd, copydataPtr: lParam))
                //            return TRUE;
                //    }
                //    break;
                case (uint)WindowMessage.WM_THEMECHANGED:
                    {
                        if (HandleMsgWmThemeChanged(hWnd))
                            return TRUE;
                    }
                    break;
            }

            return base.WndProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData);
        }

        //private bool HandleMsgWmCopyData(IntPtr hWnd, IntPtr copydataPtr)
        //{
        //    if (_window.Minimized)
        //    {
        //        // Restore original size and position only if the window is minimized, otherwise keeps the current position
        //        _ = User32.ShowWindow(hWnd, User32.SW_RESTORE);
        //    }

        //    // Regardless of the current state, try to brings into the foreground and activates the window
        //    _ = User32.SetForegroundWindow(hWnd);

        //    try
        //    {
        //        var copyDataObject = Marshal.PtrToStructure(copydataPtr, typeof(User32.COPYDATASTRUCT));
        //        if (copyDataObject is User32.COPYDATASTRUCT copyData && copyData.cbData != 0)
        //        {
        //            if (AppEnvironment.IsDiagnosticLevelVerbose)
        //                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(AppWindowSubclass) }.{ nameof(HandleMsgWmCopyData) }", content: copyData.lpData);

        //            var startupMessage = JsonSerializer.Deserialize<AppInstanceStartupMessage>(json: copyData.lpData);
        //            if (startupMessage?.IsEmpty == false)
        //            {
        //                var webMessageString = startupMessage.ToWebMessageString();
        //                _window.SendWebMessage(webMessageString);
        //            }

        //            // Here we return true because the WM_COPYDATA message has been received and processed, regardless of whether startupMessage is empty or not
        //            return true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var exceptionMessage = UnknownWebMessage.CreateFrom(ex);
        //        var exceptionMessageString = exceptionMessage.AsString;

        //        _window.SendWebMessage(exceptionMessageString);

        //        if (AppEnvironment.IsDiagnosticLevelVerbose)
        //            AppEnvironment.AddDiagnostics(name: $"{ nameof(AppWindowSubclass) }.{ nameof(HandleMsgWmCopyData) }", ex);
        //    }

        //    return false;
        //}

        private static bool HandleMsgWmThemeChanged(IntPtr hWnd)
        {
            if (UserPreferences.Current.Theme == ThemeType.Auto)
            {
                ThemeHelper.ChangeTheme(hWnd, ThemeType.Auto);
            }

            // Here we always return true to avoid that the WM_THEMECHANGED message is sent to the Photino native WndProc
            // This is due to the fact that the ThemeHelper class adds a custom non-client area color handler 
            return true;
        }
    }
}