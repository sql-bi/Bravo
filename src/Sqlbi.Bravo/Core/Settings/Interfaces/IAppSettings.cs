using Serilog.Events;
using System.Security;

namespace Sqlbi.Bravo.Core.Settings.Interfaces
{
    internal interface IAppSettings
    {
        bool TelemetryEnabled { get; set; }

        LogEventLevel TelemetryLevel { get; set; }

        bool ProxyUseSystem { get; set; }

        string ProxyAddress { get; set; }

        string ProxyUser { get; set; }

        string ProxyPassword { get; set; }

        bool UIShellBringToForegroundOnParentProcessMainWindowScreen { get; set; }
    }
}
