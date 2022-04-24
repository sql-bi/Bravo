namespace Sqlbi.Bravo.Infrastructure
{
    using Microsoft.Win32;
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Security;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.FormatDax;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    internal static class AppEnvironment
    {
        private static readonly Lazy<bool> _isInstalledPerMachineAppInstance;
        private static readonly Lazy<bool> _isInstalledPerUserAppInstance;
        private static readonly Lazy<bool> _isFrameworkDependentAppInstance;

        public static readonly string ApiAuthenticationSchema = "BravoAuth";
        public static readonly string ApiAuthenticationToken = Cryptography.GenerateSimpleToken();
        public static readonly string ApplicationManufacturer = "SQLBI";
        public static readonly string ApplicationWebsiteUrl = "https://bravo.bi";
        public static readonly string ApplicationName = "Bravo";
        public static readonly string ApplicationStoreAliasName = "BravoStore";
        public static readonly string ApplicationMainWindowTitle = "Bravo for Power BI";
        public static readonly string ApplicationInstanceUniqueName = $"{ApplicationName}-{Guid.NewGuid():D}";
        public static readonly string ApplicationRegistryKeyName = $@"SOFTWARE\{ ApplicationManufacturer }\{ ApplicationName }";
        public static readonly string ApplicationRegistryApplicationTelemetryEnableValue = "applicationTelemetryEnabled";
        public static readonly string ApplicationRegistryApplicationInstallFolderValue = "installFolder";
        public static readonly bool TelemetryEnabledDefault = true;
        public static readonly string TelemetryInstrumentationKey = "47a8970c-6293-408a-9cce-5b7b311574d3";
        public static readonly string PBIDesktopProcessName = "PBIDesktop";
        public static readonly string PBIDesktopSSASProcessImageName = "msmdsrv.exe";
        public static readonly string PBIDesktopMainWindowTitleSuffix = " - Power BI Desktop";
        public static readonly TimeSpan MSALSignInTimeout = TimeSpan.FromMinutes(5);
        public static readonly Color ThemeColorDark = ColorTranslator.FromHtml("#202020");
        public static readonly Color ThemeColorLight = ColorTranslator.FromHtml("#F3F3F3");
        public static readonly DaxLineBreakStyle FormatDaxLineBreakDefault = DaxLineBreakStyle.InitialLineBreak;

        public static readonly string[] TrustedUriHosts = new[]
        {
            "bravo.bi",
            "sqlbi.com",
            "github.com",
            "microsoft.com",
            "daxformatter.com",
            "bravorelease.blob.core.windows.net",
        };

        static AppEnvironment()
        {
            var currentProcess = Process.GetCurrentProcess();

            ProcessId = Environment.ProcessId;
            SessionId = currentProcess.SessionId;

            // use Environment.ProcessPath on .NET 6
            BravoUnexpectedException.ThrowIfNull(currentProcess.MainModule?.FileName);
            ProcessPath = currentProcess.MainModule.FileName;

            VersionInfo = FileVersionInfo.GetVersionInfo(ProcessPath);
            BravoUnexpectedException.ThrowIfNull(VersionInfo.FileVersion);
            ApplicationFileVersion = VersionInfo.FileVersion;
            BravoUnexpectedException.ThrowIfNull(VersionInfo.ProductVersion);
            ApplicationProductVersion = VersionInfo.ProductVersion;

            IsOSVersionUnsupported = Environment.OSVersion.Version < new Version(10, 0, 17763);
            IsPackagedAppInstance = DesktopBridgeHelper.IsRunningAsMsixPackage();
            ApplicationDataPath = Path.Combine(Environment.GetFolderPath(IsPackagedAppInstance ? Environment.SpecialFolder.UserProfile : Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify), ApplicationName);
            ApplicationTempPath = Path.Combine(ApplicationDataPath, ".temp");
            ApplicationDiagnosticPath = Path.Combine(ApplicationDataPath, ".diagnostic");
            UserSettingsFilePath = Path.Combine(ApplicationDataPath, "usersettings.json");
            MsalTokenCacheFilePath = Path.Combine(ApplicationDataPath, ".msalcache");
            WebView2VersionInfo = WebView2Helper.GetRuntimeVersionInfo();

            Diagnostics = new ConcurrentDictionary<string, DiagnosticMessage>();
            DefaultJsonOptions = new(JsonSerializerDefaults.Web) { MaxDepth = 32 }; // see Microsoft.AspNetCore.Mvc.JsonOptions.JsonSerializerOptions
            DefaultJsonOptions.Converters.Add(new JsonStringEnumMemberConverter()); // https://github.com/dotnet/runtime/issues/31081#issuecomment-578459083

            _isInstalledPerMachineAppInstance = new(() => IsRunningFromInstallFolder(Registry.LocalMachine));
            _isInstalledPerUserAppInstance = new(() => IsRunningFromInstallFolder(Registry.CurrentUser));
            _isFrameworkDependentAppInstance = new(() => IsFrameworkDependentPublishMode());
        }

        /// <summary>
        /// Specifies the unique identifier that spans a period of time from log in to log out and is generated by the operating system when the user's session is created.
        /// It allows us to correctly identify the ownership of the process in case multiple sessions are active or in multi-session environments such as Remote Desktop Services (a.k.a Terminal Services).
        /// </summary>
        public static int SessionId { get; }

        public static int ProcessId { get; }

        public static string ProcessPath { get; }

        public static bool IsOSVersionUnsupported { get; }

        /// <summary>
        /// Returns true if the current app istance is running as packaged application
        /// </summary>
        public static bool IsPackagedAppInstance { get; }

        /// <summary>
        /// Returns true if the current app istance was published as a framework-dependent mode
        /// </summary>
        public static bool IsFrameworkDependentAppInstance => _isFrameworkDependentAppInstance.Value;

        /// <summary>
        /// Returns true if the current app istance was installed from portable ZIP package
        /// </summary>
        public static bool IsPortableAppInstance => !IsPackagedAppInstance && !IsInstalledAppInstance;

        /// <summary>
        /// Returns true if the current app istance was installed from an MSI package
        /// </summary>
        public static bool IsInstalledAppInstance => IsInstalledPerMachineAppInstance || IsInstalledPerUserAppInstance;

        /// <summary>
        /// Returns true if the current app istance was installed from a per-machine MSI package
        /// </summary>
        public static bool IsInstalledPerMachineAppInstance => _isInstalledPerMachineAppInstance.Value;

        /// <summary>
        /// Returns true if the current app istance was installed from a per-user MSI package
        /// </summary>
        public static bool IsInstalledPerUserAppInstance => _isInstalledPerUserAppInstance.Value;

        /// <summary>
        /// Returns the HKEY registry key used to install the current application instance. Returns null if it is a packaged or portable app instance
        /// </summary>
        public static RegistryKey? ApplicationInstallerRegistryHKey => IsInstalledPerMachineAppInstance ? Registry.LocalMachine : IsInstalledPerUserAppInstance ? Registry.CurrentUser : null;

        public static string ApplicationFileVersion { get; }

        public static string ApplicationProductVersion { get; }

        public static JsonSerializerOptions DefaultJsonOptions { get; }

        public static string ApplicationDataPath { get; }

        public static string ApplicationTempPath { get; }

        public static string ApplicationDiagnosticPath { get; }

        public static string UserSettingsFilePath { get; }

        public static string MsalTokenCacheFilePath { get; }

        public static FileVersionInfo VersionInfo { get; }

        public static string? WebView2VersionInfo { get; }

        public static bool IsWebView2RuntimeInstalled => WebView2VersionInfo is not null;

        public static bool IsDiagnosticLevelVerbose => UserPreferences.Current.DiagnosticLevel == DiagnosticLevelType.Verbose;

        public static ConcurrentDictionary<string, DiagnosticMessage> Diagnostics { get; }

        public static void AddDiagnostics(string name, Exception exception, DiagnosticMessageSeverity severity = DiagnosticMessageSeverity.Error)
        {
            var content = exception.ToString();
            AddDiagnostics(DiagnosticMessageType.Text, $"{ name }({ nameof(Exception) })", content, severity);
        }

        public static void AddDiagnostics(DiagnosticMessageType type, string name, string content, DiagnosticMessageSeverity severity = DiagnosticMessageSeverity.None, bool writeFile = false)
        {
            var message = DiagnosticMessage.Create(type, severity, name, content);

            Diagnostics.AddOrUpdate(message.Name!, message, (key, value) => message);

            if (writeFile)
            {
                WriteDiagnosticFile(message);
            }
        }

        private static void WriteDiagnosticFile(DiagnosticMessage message)
        {
            if (IsDiagnosticLevelVerbose)
            {
                try
                {
                    Directory.CreateDirectory(ApplicationDiagnosticPath);

                    var extension = message.Type switch
                    {
                        DiagnosticMessageType.Text => "txt",
                        DiagnosticMessageType.Json => "json",
                        _ => "bin",
                    };

                    BravoUnexpectedException.ThrowIfNull(message.Name);

                    var name = Path.ChangeExtension(message.Name, extension);
                    var safeName = name.ReplaceInvalidFileNameChars();
                    var path = Path.Combine(ApplicationDiagnosticPath, safeName);

                    File.WriteAllText(path, message.Content);
                }
                catch (Exception ex)
                {
                    ExceptionHelper.WriteToEventLog(ex, EventLogEntryType.Warning, throwOnError: false);
                }
            }
        }

        private static bool IsRunningFromInstallFolder(RegistryKey registryKey)
        {
            if (IsPackagedAppInstance)
                return false;

            using var registrySubKey = registryKey.OpenSubKey(ApplicationRegistryKeyName, writable: false);

            if (registrySubKey is not null)
            {
                var value = registrySubKey.GetValue(ApplicationRegistryApplicationInstallFolderValue, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (value != null)
                {
                    var valueKind = registrySubKey.GetValueKind(ApplicationRegistryApplicationInstallFolderValue);
                    if (valueKind == RegistryValueKind.String)
                    {
                        var valueString = (string)value;
                        var installPath = CommonHelper.NormalizePath(valueString);
                        var runningPath = CommonHelper.NormalizePath(AppContext.BaseDirectory);

                        return installPath == runningPath;
                    }
                }
            }

            return false;
        }

        private static bool IsFrameworkDependentPublishMode()
        {
            var coreclrFound = Directory.EnumerateFiles(AppContext.BaseDirectory).Any((name) =>
            {
                return Path.GetFileName(name).EqualsI("coreclr.dll");
            });

            return !coreclrFound;
        }
    }
}
