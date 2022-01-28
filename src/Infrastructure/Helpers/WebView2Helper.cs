namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    internal static class WebView2Helper
    {
        [DllImport(ExternDll.WebView2Loader)]
        internal static extern int GetAvailableCoreWebView2BrowserVersionString([In][MarshalAs(UnmanagedType.LPWStr)] string? browserExecutableFolder, [MarshalAs(UnmanagedType.LPWStr)] ref string versionInfo);

        /// <summary>
        /// The Bootstrapper is a tiny installer that downloads the Evergreen Runtime matching device architecture and installs it locally.
        /// https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section
        /// </summary>
        public static string EvergreenRuntimeBootstrapperUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
        public static string MicrosoftReferenceUrl = "https://developer.microsoft.com/en-us/microsoft-edge/webview2";

        public static string? GetRuntimeVersionInfo()
        {
            var versionInfo = (string?)null;
#pragma warning disable CS8601 // Possible null reference assignment.
            var errorCode = GetAvailableCoreWebView2BrowserVersionString(browserExecutableFolder: null, ref versionInfo);
#pragma warning restore CS8601 // Possible null reference assignment.
            if (errorCode == (int)HRESULT.E_FILENOTFOUND)
            {
                // WebView2 runtime not found
                return null;
            }

            Marshal.ThrowExceptionForHR(errorCode);
            return versionInfo;
        }

        public static void EnsureRuntimeIsInstalled()
        {
            if (AppConstants.IsWebView2RuntimeInstalled)
                return;

            var appIcon = Icon.ExtractAssociatedIcon(AppConstants.MainModuleFileName);
            var icon = new TaskDialogIcon(appIcon!);

            var page = new TaskDialogPage()
            {
                Caption = AppConstants.ApplicationMainWindowTitle,
                Heading = @$"
{ AppConstants.ApplicationMainWindowTitle } requires the Microsoft Edge WebView2 runtime which is not currently installed.

Choose an option to proceed with the installation:",
                Icon = icon,
                AllowCancel = false,
                Footnote = new TaskDialogFootnote()
                {
                    Text = $"For more details please refer to the following address:\r\n\r\n - { AppConstants.ApplicationWebsiteUrl }\r\n - { MicrosoftReferenceUrl }",
                },
                Buttons =
                {
                    new TaskDialogCommandLinkButton("&Automatic", "Download and install Microsoft Edge WebView2 runtime now")
                    {
                        Tag = 10
                    },
                    new TaskDialogCommandLinkButton("&Manual", "Open the browser on the download page")
                    {
                        Tag = 20
                    },
                    new TaskDialogCommandLinkButton("&Cancel", "Close the application without installing")
                    {
                        Tag = 30,
                    }
                },
                //Expander = new TaskDialogExpander()
                //{
                //    Text = " ... ",
                //    Position = TaskDialogExpanderPosition.AfterFootnote
                //}
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var hwndOwner = ProcessHelper.GetParentProcessMainWindowHandle();
            var dialogButton = TaskDialog.ShowDialog(hwndOwner, page, TaskDialogStartupLocation.CenterScreen);

            switch (dialogButton.Tag)
            {
                case 10:
                    DownloadAndInstallRuntime();
                    break;
                case 20:
                    _ = ProcessHelper.OpenInBrowser(new Uri(MicrosoftReferenceUrl, uriKind: UriKind.Absolute));
                    break;
                case 30:
                    // default to Environment.Exit
                    break;
                default:
                    throw new BravoUnexpectedException($"TaskDialog result '{ dialogButton.Tag }'");
            }

            Environment.Exit(NativeMethods.NO_ERROR);
        }

        private static void DownloadAndInstallRuntime()
        {
            // TODO: use http client from pool, add proxy support
            using var httpClient = new HttpClient();

            var fileBytes = httpClient.GetByteArrayAsync(EvergreenRuntimeBootstrapperUrl).GetAwaiter().GetResult();
            var filePath = Path.Combine(AppConstants.ApplicationTempPath, $"MicrosoftEdgeWebview2Setup-{DateTime.Now:yyyyMMddHHmmss}.exe");

            File.WriteAllBytes(filePath, fileBytes);

            using var process = Process.Start(filePath); // add switches ? i.e. /silent /install
            process.WaitForExit();

            if (process.ExitCode != NativeMethods.NO_ERROR)
            {
                ExceptionHelper.WriteToEventLog($"WebView2 bootstrapper exit code '{ process.ExitCode }'", EventLogEntryType.Warning);
            }
        }
    }
}
