using Dax.Formatter.Models;
using Serilog.Events;

namespace Sqlbi.Bravo.Core.Settings.Interfaces
{
    internal interface IAppSettings
    {
        bool TelemetryEnabled { get; set; }

        bool ProxyUseSystem { get; set; }

        string ProxyAddress { get; set; }

        string ProxyUser { get; set; }

        string ProxyPassword { get; set; }

        bool ShellBringToForegroundOnParentProcessMainWindowScreen { get; set; }

        string ThemeName { get; set; }

        DaxFormatterLineStyle DaxFormatterLineStyle { get; set; }
    }
}
