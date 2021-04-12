using Dax.Formatter.Models;
using Sqlbi.Bravo.Core.Settings.Interfaces;

namespace Sqlbi.Bravo.Core.Settings
{
    internal class AppSettings : IAppSettings
    {
        private bool _proxyUseSystem = AppConstants.ApplicationSettingsDefaultProxyUseSystem;

        public bool TelemetryEnabled { get; set; } = AppConstants.ApplicationSettingsDefaultTelemetryEnabled;

        public bool ProxyUseSystem
        { 
            get => _proxyUseSystem; 
            set
            {
                if (_proxyUseSystem = value)
                {
                    ProxyAddress = null;
                    ProxyUser = null;
                    ProxyPassword = null;
                }
            }
        } 

        public string ProxyAddress { get; set; }

        public string ProxyUser { get; set; }

        public string ProxyPassword { get; set; }

        public bool ShellBringToForegroundOnParentProcessMainWindowScreen { get; set; } = AppConstants.ApplicationSettingsDefaultShellBringToForegroundOnParentProcessMainWindowScreen;

        public string ThemeName { get; set; } = AppConstants.ApplicationSettingsDefaultThemeName;

        public DaxFormatterLineStyle DaxFormatterLineStyle { get; set; } = AppConstants.ApplicationSettingsDefaultDaxFormatterLineStyle;
    }
}
