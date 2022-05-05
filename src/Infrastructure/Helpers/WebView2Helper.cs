namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net;
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

        /// <summary>
        /// Additional environment variables verified when WebView2Environment is created.
        /// If additional browser arguments is specified in environment variable or in the registry, it is appended to the corresponding values in CreateCoreWebView2EnvironmentWithOptions parameters.
        /// https://docs.microsoft.com/en-us/microsoft-edge/webview2/reference/win32/webview2-idl
        /// </summary>
        private const string EnvironmentVariableAdditionalBrowserArguments = "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS";

        public static string? GetRuntimeVersionInfo()
        {
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
        }

        public static void EnsureRuntimeIsInstalled()
        {
            if (AppEnvironment.IsWebView2RuntimeInstalled)
                return;

            var appIcon = Icon.ExtractAssociatedIcon(AppEnvironment.ProcessPath);
            var icon = new TaskDialogIcon(appIcon!);

            var page = new TaskDialogPage()
            {
                Caption = AppEnvironment.ApplicationMainWindowTitle,
                Heading = @$"{ AppEnvironment.ApplicationMainWindowTitle } requires the Microsoft Edge WebView2 runtime which is not currently installed.

Choose an option to proceed with the installation:",
                Icon = icon,
                AllowCancel = false,
                Footnote = new TaskDialogFootnote()
                {
                    Text = $"For more details please refer to the following address:\r\n\r\n - { AppEnvironment.ApplicationWebsiteUrl }\r\n - { MicrosoftReferenceUrl }",
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

            var dialogButton = TaskDialog.ShowDialog(page, TaskDialogStartupLocation.CenterScreen);

            switch (dialogButton.Tag)
            {
                case 10:
                    DownloadAndInstallRuntime();
                    break;
                case 20:
                    _ = ProcessHelper.OpenBrowser(new Uri(MicrosoftReferenceUrl, uriKind: UriKind.Absolute));
                    break;
                case 30:
                    // default to Environment.Exit
                    break;
                default:
                    throw new BravoUnexpectedException($"Unexpected { nameof(TaskDialogButton) } result ({ dialogButton.Tag })");
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

        public static void SetWebView2CmdlineProxyArguments(ProxySettings? proxySettings, IWebProxy systemProxy)
        {
            // Command-line options for proxy settings
            // https://docs.microsoft.com/en-us/deployedge/edge-learnmore-cmdline-options-proxy-settings#command-line-options-for-proxy-settings

            var proxyArguments = (proxySettings?.Type) switch
            {
                ProxyType.None => "--no-proxy-server",
                ProxyType.Custom => GetCustomProxyArguments(proxySettings),
                _ => GetSystemProxyArguments(systemProxy),
            };

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Text, name: $"{ nameof(WebView2Helper) }.{ nameof(SetWebView2CmdlineProxyArguments) }", content: proxyArguments);

            Environment.SetEnvironmentVariable(EnvironmentVariableAdditionalBrowserArguments, proxyArguments, EnvironmentVariableTarget.Process);

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

                    var arguments = string.Empty;
                    {
                        var server = "{0};{1}".FormatInvariant(httpProxyUri, httpsProxyUri).Trim(';');
                        if (server.Length > 0)
                        {
                            arguments += ' ' + "--proxy-server=\"{0}\"".FormatInvariant(server);
                        }

                        var bypassList = string.Join(';', ProxySettings.GetSafeBypassList(bypass, includeLoopback: true));
                        if (bypassList.Length > 0)
                        {
                            arguments += ' ' + "--proxy-bypass-list=\"{0}\"".FormatInvariant(bypassList);
                        }
                    }
                    return arguments;
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

                    var arguments = string.Empty;
                    {
                        if (proxy?.Length > 0)
                        {
                            arguments += ' ' + "--proxy-server=\"{0}\"".FormatInvariant(proxy);
                        }

                        var bypassList = string.Join(';', ProxySettings.GetSafeBypassList(bypass, includeLoopback: true));
                        if (bypassList.Length > 0)
                        {
                            arguments += ' ' + "--proxy-bypass-list=\"{0}\"".FormatInvariant(bypassList);
                        }

                        if (autoConfigUrl?.Length > 0)
                        {
                            arguments += ' ' + "--proxy-pac-url=\"{0}\"".FormatInvariant(autoConfigUrl);
                        }
                    }
                    return arguments;
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