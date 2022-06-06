namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Microsoft.Web.WebView2.Core;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Windows;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Windows.Forms;

    internal static class WebView2Helper
    {
        //[DllImport(ExternDll.WebView2Loader)]
        //internal static extern int GetAvailableCoreWebView2BrowserVersionString([In][MarshalAs(UnmanagedType.LPWStr)] string? browserExecutableFolder, [MarshalAs(UnmanagedType.LPWStr)] ref string versionInfo);

        /// <summary>
        /// The Bootstrapper is a tiny installer that downloads the Evergreen Runtime matching device architecture and installs it locally.
        /// https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section
        /// </summary>
        public static string EvergreenRuntimeBootstrapperUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
        public static string MicrosoftReferenceUrl = "https://developer.microsoft.com/en-us/microsoft-edge/webview2";

        public static void TryAndIgnoreUnsupportedInterfaceError(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (InvalidCastException ex) when (ex.HResult == HRESULT.E_NOINTERFACE)
            {
                // Ignore error if feature is unsupported
                //
                // Feature-detecting to test whether the installed Runtime supports recently added APIs
                // https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/versioning#feature-detecting-to-test-whether-the-installed-runtime-supports-recently-added-apis
                //
            }
        }

        public static string? GetRuntimeVersionInfo()
        {
            try
            {
                var versionInfo = CoreWebView2Environment.GetAvailableBrowserVersionString(browserExecutableFolder: null);
                return versionInfo;
            }
            catch (WebView2RuntimeNotFoundException)
            {
                return null;
            }
/*
            var versionInfo = (string?)null;
#pragma warning disable CS8601 // Possible null reference assignment.
            var errorCode = GetAvailableCoreWebView2BrowserVersionString(browserExecutableFolder: null, ref versionInfo);
#pragma warning restore CS8601 // Possible null reference assignment.
            if (errorCode == HRESULT.E_FILENOTFOUND)
            {
                // WebView2 runtime not found
                return null;
            }

            Marshal.ThrowExceptionForHR(errorCode);
            return versionInfo;
*/
        }

        public static void EnsureRuntimeIsInstalled()
        {
            if (AppEnvironment.IsWebView2RuntimeInstalled)
                return;

            var heading = $"{ AppEnvironment.ApplicationMainWindowTitle } requires the Microsoft Edge WebView2 runtime which is not currently installed.\r\n\r\nChoose an option to proceed with the installation:";
            var footnoteText = $"For more details please refer to the following address:\r\n\r\n - { AppEnvironment.ApplicationWebsiteUrl }\r\n - { MicrosoftReferenceUrl }";
            var automaticButton = new TaskDialogCommandLinkButton("&Automatic", "Download and install Microsoft Edge WebView2 runtime now");
            var manualButton = new TaskDialogCommandLinkButton("&Manual", "Open the browser on the download page");
            var cancelButton = new TaskDialogCommandLinkButton("&Cancel", "Close the application without installing");

            var dialogButton = MessageDialog.ShowDialog(heading, text: null, footnoteText, allowCancel: false, automaticButton, manualButton, cancelButton);

            if (dialogButton == automaticButton)
            {
                DownloadAndInstallRuntime();
            }
            else if (dialogButton == manualButton)
            {
                var address = new Uri(MicrosoftReferenceUrl, uriKind: UriKind.Absolute);
                _ = ProcessHelper.OpenBrowser(address);
            }
            else if (dialogButton == cancelButton)
            {
                //
            }

            Environment.Exit(NativeMethods.NO_ERROR);
        }

        private static void DownloadAndInstallRuntime()
        {
            // TODO: use http client from pool, add proxy support
            using var httpClient = new HttpClient();

            var fileBytes = httpClient.GetByteArrayAsync(EvergreenRuntimeBootstrapperUrl).GetAwaiter().GetResult();
            var filePath = Path.Combine(AppEnvironment.ApplicationTempPath, $"MicrosoftEdgeWebview2Setup-{DateTime.Now:yyyyMMddHHmmss}.exe");

            File.WriteAllBytes(filePath, fileBytes);

            using var process = Process.Start(filePath); // add switches ? i.e. /silent /install
            process.WaitForExit();

            if (process.ExitCode != NativeMethods.NO_ERROR)
            {
                ExceptionHelper.WriteToEventLog($"WebView2 bootstrapper exit code '{ process.ExitCode }'", EventLogEntryType.Warning);
            }
        }

        public static string GetProxyArguments(ProxySettings? proxySettings, IWebProxy systemProxy)
        {
            // Command-line options for proxy settings
            // https://docs.microsoft.com/en-us/deployedge/edge-learnmore-cmdline-options-proxy-settings#command-line-options-for-proxy-settings

            var proxyArguments = (proxySettings?.Type) switch
            {
                ProxyType.None => "--no-proxy-server",
                ProxyType.Custom => GetCustomProxyArguments(proxySettings),
                _ => GetSystemProxyArguments(systemProxy),
            };

            return proxyArguments;

            static string GetCustomProxyArguments(ProxySettings proxySettings)
            {
                var server = proxySettings.Address;
                var bypassList = string.Join(';', ProxySettings.GetSafeBypassList(proxySettings.BypassList, includeLoopback: true));
                var arguments = "--proxy-server=\"{0}\" --proxy-bypass-list=\"{1}\"".FormatInvariant(server, bypassList);

                return arguments;
            }

            static string GetSystemProxyArguments(IWebProxy systemProxy)
            {
                var systemProxyType = systemProxy.GetType();

                if (systemProxyType.FullName == "System.Net.Http.HttpEnvironmentProxy")
                {
                    string[]? bypass = null;
                    Uri? httpsProxyUri = null;
                    Uri? httpProxyUri = null;

                    var bypassObject = systemProxyType.GetField("_bypass", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(systemProxy);
                    if (bypassObject is IEnumerable<string> items)
                        bypass = items.ToArray();

                    var httpProxyUriObject = systemProxyType.GetField("_httpProxyUri", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(systemProxy);
                    if (httpProxyUriObject is Uri httpUri)
                        httpProxyUri = httpUri;

                    var httpsProxyUriObject = systemProxyType.GetField("_httpsProxyUri", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(systemProxy);
                    if (httpsProxyUriObject is Uri httpsUri)
                        httpsProxyUri = httpsUri;

                    var arguments = new List<string>();
                    {
                        var server = "{0};{1}".FormatInvariant(httpProxyUri, httpsProxyUri).Trim(';');
                        if (server.Length > 0)
                        {
                            arguments.Add("--proxy-server=\"{0}\"".FormatInvariant(server));
                        }

                        var bypassList = string.Join(';', ProxySettings.GetSafeBypassList(bypass, includeLoopback: true));
                        if (bypassList.Length > 0)
                        {
                            arguments.Add("--proxy-bypass-list=\"{0}\"".FormatInvariant(bypassList));
                        }
                    }

                    var proxyArguments = string.Join(' ', arguments);
                    return proxyArguments;
                }
                else if (systemProxyType.FullName == "System.Net.Http.HttpWindowsProxy")
                {
                    string[]? bypass = null;
                    string? proxy = null;
                    string? autoConfigUrl = null;

                    var bypassObject = systemProxyType.GetField("_bypass", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(systemProxy);
                    if (bypassObject is IEnumerable<string> items)
                        bypass = items.ToArray();

                    var proxyHelperObject = systemProxyType.GetField("_proxyHelper", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(systemProxy);
                    var proxyHelperType = proxyHelperObject?.GetType();
                    if (proxyHelperType?.FullName == "System.Net.Http.WinInetProxyHelper")
                    {
                        var proxyObject = proxyHelperType.GetField("_proxy", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(proxyHelperObject);
                        if (proxyObject is string proxyValue)
                            proxy = proxyValue;

                        var autoConfigUrlObject = proxyHelperType.GetField("_autoConfigUrl", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(proxyHelperObject);
                        if (autoConfigUrlObject is string autoConfigUrlValue)
                            autoConfigUrl = autoConfigUrlValue;
                    }

                    var arguments = new List<string>();
                    {
                        if (proxy?.Length > 0)
                        {
                            arguments.Add("--proxy-server=\"{0}\"".FormatInvariant(proxy));
                        }

                        var bypassList = string.Join(';', ProxySettings.GetSafeBypassList(bypass, includeLoopback: true));
                        if (bypassList.Length > 0)
                        {
                            arguments.Add("--proxy-bypass-list=\"{0}\"".FormatInvariant(bypassList));
                        }

                        if (autoConfigUrl?.Length > 0)
                        {
                            arguments.Add("--proxy-pac-url=\"{0}\"".FormatInvariant(autoConfigUrl));
                        }
                    }

                    var proxyArguments = string.Join(' ', arguments);
                    return proxyArguments;
                }
                else if (systemProxyType.FullName == "System.Net.Http.HttpNoProxy")
                {
                    return "--no-proxy-server";
                }
                else
                {
                    throw new BravoUnexpectedException($"Unexpected { nameof(IWebProxy) } type ({ systemProxyType.FullName })");
                }
            }
        }
    }
}