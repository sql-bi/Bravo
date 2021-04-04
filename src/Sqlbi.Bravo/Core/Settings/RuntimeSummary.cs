using Sqlbi.Bravo.Core.Helpers;

namespace Sqlbi.Bravo.Core.Settings
{
    /// <summary>
    /// This is a selection of the information from RuntimeSettings that is held by each tab.
    /// If different tabs coudn't connect to different data sources then could access `settings.Runtime.Xxxx` directly.
    /// </summary>
    internal class RuntimeSummary
    {
        public string ServerName { get; set; }

        public string DatabaseName { get; set; }

        public string ParentProcessName { get; set; }

        public string ParentProcessMainWindowTitle { get; set; }

        public bool IsExecutedAsExternalTool { get; set; }

        public bool UsingLocalModelForAnanlysis { get; internal set; } = false;

        public string ConnectionString => AnalysisServicesHelper.BuildConnectionString(ServerName, DatabaseName);
    }
}