using PhotinoNET;
using Sqlbi.Bravo.Infrastructure;
using System;
using System.Diagnostics;

namespace Sqlbi.Bravo
{
    internal partial class Program
    {
        private static PhotinoWindow CreateWindow()
        {
#if DEBUG
            var contextMenuEnabled = true;
#else
            var contextMenuEnabled = false;
#endif
            var window = new PhotinoWindow()
                .SetTitle(AppConstants.ApplicationMainWindowTitle)
                .SetIconFile("wwwroot/bravo.ico")
                .SetContextMenuEnabled(contextMenuEnabled)
                .SetGrantBrowserPermissions(true)
                .SetUseOsDefaultSize(true)
                .RegisterWebMessageReceivedHandler(WindowWebMessageReceived)
                .Load("wwwroot/index.html");

            window.RegisterWebMessageReceivedHandler(WindowWebMessageReceived);
            window.RegisterWindowCreatingHandler(WindowCreating);
            window.RegisterWindowCreatedHandler(WindowCreated);
            window.RegisterWindowClosingHandler(WindowClosing);
#if DEBUG
            window.SetLogVerbosity(3); // 0 = Critical Only, 1 = Critical and Warning, 2 = Verbose, >2 = All Details. Default is 2.
#else
            window.SetLogVerbosity(1);
#endif
            window.WindowCreated += (sender, e) => _singleton.TryHookWndProc(sender);
            return window;
        }

        private static void WindowCreating(object? sender, EventArgs e)
        {
            var window = (PhotinoWindow)sender!;
            Trace.WriteLine($"::Bravo:INF:WindowCreatingHandler:{ window.Title }");
        }

        private static void WindowCreated(object? sender, EventArgs e)
        {
            var window = (PhotinoWindow)sender!;
            Trace.WriteLine($"::Bravo:INF:WindowCreatedHandler:{ window.Title } ( { _hostUri } )");

            //window.SendNotification("::Bravo:INF:WindowCreatedHandler", window.Title);
        }

        private static bool WindowClosing(object sender, EventArgs args)
        {
            Trace.WriteLine($"::Bravo:INF:WindowClosingHandler");
            return false; // Could return true to stop windows close
        }

        private static void WindowWebMessageReceived(object? sender, string message)
        {
            Trace.WriteLine($"::Bravo:INF:WebMessageReceived:{ message }");

            var window = (PhotinoWindow)sender!;

            if (message == "host-address")
            {
                var address = _hostUri?.ToString();
                window.SendWebMessage($"{ _hostUri }");
            }
            else
            {
                // TODO: log
            }
        }
    }
}
