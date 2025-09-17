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
    using System.Text.Json;

    internal static class AppEnvironment
    {
        private static readonly Lazy<AppDeploymentMode> _deploymentMode;

        public static readonly string ApiAuthenticationSchema = "BravoAuth";
        public static readonly string ApiAuthenticationToken = Cryptography.GenerateSimpleToken();
        public static readonly string ApiAuthenticationTokenTemplateDevelopment = Cryptography.GenerateSimpleToken();
        public static readonly string ApplicationManufacturer = "SQLBI";
        public static readonly string ApplicationWebsiteUrl = "https://bravo.bi";
        public static readonly string ApplicationName = "Bravo";
        public static readonly string ApplicationStoreAliasName = "BravoStore";
        public static readonly string ApplicationMainWindowTitle = "Bravo for Power BI";
        public static readonly string ApplicationInstanceUniqueName = $"{ApplicationName}-{Guid.NewGuid():D}";
        public static readonly string ApplicationRegistryKeyName = $@"SOFTWARE\{ ApplicationManufacturer }\{ ApplicationName }";
        public static readonly string ApplicationRegistryApplicationTelemetryEnabledValue = "applicationTelemetryEnabled";
        public static readonly string ApplicationRegistryApplicationTitleVersionHiddenValue = "applicationTitleVersionHidden";
        public static readonly string ApplicationRegistryApplicationInstallFolderValue = "installFolder";
        public static readonly string TelemetryInstrumentationKey = "47a8970c-6293-408a-9cce-5b7b311574d3";
        public static readonly string TelemetryConnectionString = "InstrumentationKey=47a8970c-6293-408a-9cce-5b7b311574d3";
        public static readonly string PBIDesktopProcessName = "PBIDesktop";
        public static readonly string PBIDesktopSSASProcessImageName = "msmdsrv.exe";
        public static readonly string[] PBIDesktopMainWindowTitleSuffixes = new string[]
        {
            // The PBIDesktop main window title is culture-specific.
            // The suffix is not always present, for example, it is not added when the save/share function in OneDrive/SharePoint is active.

            // Different dash characters are used as a separator
            // See https://github.com/sql-bi/Bravo/issues/476
            " \u002D Power BI Desktop", // Dash Punctuation - minus hyphen
            " \u2013 Power BI Desktop", // Dash Punctuation - en dash
            " \u2014 Power BI Desktop", // Dash Punctuation - em dash

            // The whitespace character may not be present - Swedish/sv
            // See https://github.com/sql-bi/Bravo/issues/510
            "\u2013Power BI Desktop", // Dash Punctuation - en dash
            
            // NBSP char instead of whitespace - Latvian/lv
            "\u00A0\u2014 Power BI Desktop",
        };
        public static readonly TimeSpan MSALSignInTimeout = TimeSpan.FromMinutes(5);
        public static readonly Color ThemeColorDark = ColorTranslator.FromHtml("#202020");
        public static readonly Color ThemeColorLight = ColorTranslator.FromHtml("#F3F3F3");
        public static readonly DaxLineBreakStyle FormatDaxLineBreakDefault = DaxLineBreakStyle.InitialLineBreak;
        public static readonly string CredentialManagerProxyCredentialName = "Bravo for Power BI/proxy";

        public static readonly string[] TrustedUriHosts = new[]
        {
            "bravo.bi",
            "sqlbi.com",
            "github.com",
            "microsoft.com",
            "daxformatter.com",
            "bravorelease.blob.core.windows.net",
            "code.visualstudio.com",
            "marketplace.visualstudio.com"
        };

        static AppEnvironment()
        {
            Debug.Assert(Environment.ProcessPath is not null);

            _deploymentMode = new(() => GetDeploymentMode());
            using var currentProcess = Process.GetCurrentProcess();

            ProcessId = Environment.ProcessId;
            SessionId = currentProcess.SessionId;
            ProcessPath = Environment.ProcessPath!;
            
            VersionInfo = FileVersionInfo.GetVersionInfo(ProcessPath);
            BravoUnexpectedException.ThrowIfNull(VersionInfo.FileVersion);
            ApplicationFileVersion = VersionInfo.FileVersion;
            BravoUnexpectedException.ThrowIfNull(VersionInfo.ProductVersion);
            ApplicationProductVersion = VersionInfo.ProductVersion;

            ApplicationDataPath = Path.Combine(Environment.GetFolderPath(DeploymentMode == AppDeploymentMode.Packaged ? Environment.SpecialFolder.UserProfile : Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify), ApplicationName);
            ApplicationTempPath = Path.Combine(ApplicationDataPath, ".temp");
            ApplicationDiagnosticPath = Path.Combine(ApplicationDataPath, ".diagnostic");
            UserSettingsFilePath = Path.Combine(ApplicationDataPath, "usersettings.json");
            MsalTokenCacheFilePath = Path.Combine(ApplicationDataPath, ".msalcache");
            WebView2VersionInfo = WebView2Helper.GetRuntimeVersionInfo();

            Diagnostics = new ConcurrentDictionary<DiagnosticMessage, DiagnosticMessage>();
            DefaultJsonOptions = new(JsonSerializerDefaults.Web) { MaxDepth = 32 }; // see Microsoft.AspNetCore.Mvc.JsonOptions.JsonSerializerOptions

            var spaceChars = new[]
            {
                "",       // no space
                "\u0020", // whitespace
                "\u00A0"  // nbsp
            };
            var dashChars = new[]
            {
                "\u002D", // Dash Punctuation - minus hyphen
                "\u2212", // Math Symbol - minus sign
                "\u2011", // Dash Punctuation - non-breaking hyphen
                "\u2013", // Dash Punctuation - en dash
                "\u2014", // Dash Punctuation - em dash
                "\u2015", // Dash Punctuation - horizontal bar
            };
        }

        /// <summary>
        /// Specifies the unique identifier that spans a period of time from log in to log out and is generated by the operating system when the user's session is created.
        /// It allows us to correctly identify the ownership of the process in case multiple sessions are active or in multi-session environments such as Remote Desktop Services (a.k.a Terminal Services).
        /// </summary>
        public static int SessionId { get; }

        public static int ProcessId { get; }

        public static string ProcessPath { get; }

        // TODO: use custom defined constant to identify a stable release. See PublishMode property and "AdditionalConstants" in csproj
        public static bool IsStableRelease => Version.TryParse(ApplicationProductVersion, out _);

        public static AppPublishMode PublishMode
        {
            get
            {
#if SELFCONTAINED
                return AppPublishMode.SelfContained;
#elif FRAMEWORKDEPENDENT
                return AppPublishMode.FrameworkDependent;
#else
                return AppPublishMode.None;
#endif
            }
        }

        public static AppDeploymentMode DeploymentMode => _deploymentMode.Value;

        /// <summary>
        /// Returns the HKEY registry key used to install the current application instance. Returns null if it is a packaged or portable app instance
        /// </summary>
        public static RegistryKey? ApplicationInstallerRegistryHKey
        {
            get
            {
                var registryKey = DeploymentMode switch
                {
                    AppDeploymentMode.PerUser => Registry.CurrentUser,
                    AppDeploymentMode.PerMachine => Registry.LocalMachine,
                    _ => null,
                };

                return registryKey;
            }
        }

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

        public static ConcurrentDictionary<DiagnosticMessage, DiagnosticMessage> Diagnostics { get; }

        public static void AddDiagnostics(string name, Exception exception, DiagnosticMessageSeverity severity = DiagnosticMessageSeverity.Error)
        {
            var content = exception.ToString();
            AddDiagnostics(DiagnosticMessageType.Text, name, content, severity);
        }

        public static void AddDiagnostics(DiagnosticMessageType type, string name, string content, DiagnosticMessageSeverity severity = DiagnosticMessageSeverity.None, bool writeFile = false)
        {
            var message = DiagnosticMessage.Create(type, severity, name, content);
            
            _= Diagnostics.TryAdd(message, message);

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

        private static AppDeploymentMode GetDeploymentMode()
        {
            if (DesktopBridgeHelper.IsRunningAsMsixPackage())
                return AppDeploymentMode.Packaged;

            var hklmValueString = Registry.LocalMachine.GetStringValue(subkeyName: ApplicationRegistryKeyName, valueName: ApplicationRegistryApplicationInstallFolderValue);
            if (hklmValueString is not null)
            {
                if (CommonHelper.AreDirectoryPathsEqual(AppContext.BaseDirectory, hklmValueString))
                    return AppDeploymentMode.PerMachine;
            }

            var hkcuValueString = Registry.CurrentUser.GetStringValue(subkeyName: ApplicationRegistryKeyName, valueName: ApplicationRegistryApplicationInstallFolderValue);
            if (hkcuValueString is not null)
            {
                if (CommonHelper.AreDirectoryPathsEqual(AppContext.BaseDirectory, hkcuValueString))
                    return AppDeploymentMode.PerUser;
            }

            return AppDeploymentMode.Portable;
        }
    }

    public enum AppDeploymentMode
    {
        None = 0,

        /// <summary>
        /// Portable ZIP package
        /// </summary>
        Portable = 1,

        /// <summary>
        /// MSI package per-user installation that does not require elevated privileges to install
        /// </summary>
        PerUser = 2,

        /// <summary>
        /// MSI package per-machine installation that requires elevated privileges to install
        /// </summary>
        PerMachine = 3,

        /// <summary>
        /// MSIX packaged application
        /// </summary>
        Packaged = 4,
    }

    public enum AppPublishMode
    {
        None = 0,
        SelfContained = 1,
        FrameworkDependent = 2,
    }
}
