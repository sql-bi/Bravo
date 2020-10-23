using Serilog.Events;
using Sqlbi.Bravo.Core.Settings.Interfaces;

namespace Sqlbi.Bravo.Core.Settings
{
    internal class AppSettings : IAppSettings
    {
        private bool _proxyUseSystem = AppConstants.ApplicationSettingsDefaultProxyUseSystem;

        public bool TelemetryEnabled { get; set; } = AppConstants.ApplicationSettingsDefaultTelemetryEnabled;

        public LogEventLevel TelemetryLevel { get; set; } = AppConstants.ApplicationSettingsDefaultTelemetryLevel;

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

        public bool UIShellBringToForegroundOnParentProcessMainWindowScreen { get; set; } = AppConstants.ApplicationSettingsDefaultUIShellBringToForegroundOnParentProcessMainWindowScreen;
    }
}
