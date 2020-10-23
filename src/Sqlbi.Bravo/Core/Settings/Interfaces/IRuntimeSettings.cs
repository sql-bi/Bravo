using System;
using System.Collections.Generic;

namespace Sqlbi.Bravo.Core.Settings.Interfaces
{
    internal interface IRuntimeSettings
    {
        string ServerName { get; }

        string DatabaseName { get; }

        int ParentProcessId { get; }

        string ParentProcessName { get; }

        string ParentProcessMainWindowTitle { get; }

        public IntPtr ParentProcessMainWindowHandle { get; }

        bool IsExecutedAsExternalTool { get; }

        bool IsExecutedAsExternalToolForPowerBIDesktop { get; }

        string ExternalToolInstanceId { get; }

        bool HasCommandLineParseErrors { get; }

        IReadOnlyCollection<string> CommandLineParseErrors { get; }
    }
}
