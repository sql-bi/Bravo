using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Windows.Dialogs;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;

namespace Sqlbi.Bravo.Infrastructure.Helpers;

internal static class WebView2Helper
{
    //[DllImport(ExternDll.WebView2Loader)]
    //internal static extern int GetAvailableCoreWebView2BrowserVersionString([In][MarshalAs(UnmanagedType.LPWStr)] string? browserExecutableFolder, [MarshalAs(UnmanagedType.LPWStr)] ref string versionInfo);

    /// <summary>
    /// The bootstrapper URL Microsoft provides to download the Evergreen WebView2 Runtime.
    /// </summary>
    private const string BootstrapperDownloadUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

    /// <summary>
    /// The page Microsoft addresses to end users who need to install the runtime themselves.
    /// </summary>
    private const string ConsumerDownloadPageUrl = "https://developer.microsoft.com/microsoft-edge/webview2/consumer/";

    public static void TryAndIgnoreUnsupportedError(Action action)
    {
        //
        // Feature-detecting to test whether the installed Runtime supports recently added APIs
        // https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/versioning#feature-detecting-to-test-whether-the-installed-runtime-supports-recently-added-apis
        //
        try
        {
            action?.Invoke();
        }
        catch (NotImplementedException ex) when (ex.InnerException is InvalidCastException innerEx && innerEx.HResult == HRESULT.E_NOINTERFACE)
        {
            // Ignore unsupported feature
        }
        catch (InvalidCastException ex) when (ex.HResult == HRESULT.E_NOINTERFACE)
        {
            // Ignore unsupported feature
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

    /// <summary>
    /// Ensures that the WebView2 Runtime is installed. If not, prompts the user to download and install it.
    /// </summary>
    public static void EnsureRuntimeIsInstalled()
    {
        if (AppEnvironment.IsWebView2RuntimeInstalled)
            return;

        var downloadButton = new TaskDialogCommandLinkButton("&Download it now", "You will need to run the downloaded installer, then start Bravo again.");

        var clickedButton = TaskDialogBuilder.Create()
            .WithCaption(AppEnvironment.ApplicationMainWindowTitle)
            .WithCurrentProcessIcon()
            .WithStartupLocation(TaskDialogStartupLocation.CenterScreen)
            .WithAllowCancel()
            .WithSizeToContent()
            .WithEnableLinks(OpenBrowser)
            .WithHeading("You must install WebView2 Runtime to run this application.")
            .WithText("Bravo needs Microsoft Edge WebView2 Runtime to display its user interface.")
            .WithExpander(GetDetails(), expanded: false, TaskDialogExpanderPosition.AfterText, expandedButtonText: "Hide details", collapsedButtonText: "Show details")
            .AddButtons(downloadButton, TaskDialogButton.Close)
            .WithDefaultButton(downloadButton)
            .Show();

        if (clickedButton == downloadButton)
            OpenBrowser(BootstrapperDownloadUrl);

        // The application cannot run without WebView2 Runtime, so exit with a specific error code
        Environment.Exit(NativeMethods.ERROR_CANCELLED);

        static string GetDetails()
            => $"""
               Architecture: {RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()}
               Windows version: {Environment.OSVersion.Version}
               Bravo version: {AppVersion.SemanticVersion}

               Learn more:
               <a href="{ConsumerDownloadPageUrl}">{ConsumerDownloadPageUrl}</a>

               Download link:
               <a href="{BootstrapperDownloadUrl}">{BootstrapperDownloadUrl}</a>
               """;

        static void OpenBrowser(string url)
            => ProcessHelper.OpenBrowser(new Uri(url, UriKind.Absolute));
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
                throw new BravoUnexpectedException($"Unexpected {nameof(IWebProxy)} type ({systemProxyType.FullName})");
            }
        }
    }
}
