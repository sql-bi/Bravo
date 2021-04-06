using Sqlbi.Bravo.Client.PowerBI.Desktop;
using Sqlbi.Bravo.Core.Helpers;

namespace Sqlbi.Bravo.Core.Settings
{
    /// <summary>
    /// This is a selection of the information from RuntimeSettings that is held by each tab.
    /// If different tabs coudn't connect to different data sources then could access `settings.Runtime.Xxxx` directly.
    /// </summary>
    internal class RuntimeSummary
    {
        public static RuntimeSummary CreateFrom(RuntimeSettings settings)
        {
            var runtimeSummary = new RuntimeSummary
            {
                ServerName = settings.ServerName,
                DatabaseName = settings.DatabaseName,
                ConnectionName = settings.ParentProcessMainWindowTitle,
                IsExecutedAsExternalTool = settings.IsExecutedAsExternalTool,
            };

            return runtimeSummary;
        }

        public static RuntimeSummary CreateFrom(PowerBIDesktopInstance instance)
        {
            var runtimeSummary = new RuntimeSummary
            {
                ServerName = instance.ServerName,
                DatabaseName = instance.DatabaseName,
                ConnectionName = instance.Name,
                IsExecutedAsExternalTool = false,
            };

            return runtimeSummary;
        }        

        public string ServerName { get; init; }

        public string DatabaseName { get; init; }

        public string ConnectionName { get; init; }

        public bool IsExecutedAsExternalTool { get; init; }

        public bool UsingLocalModelForAnanlysis { get; internal set; } = false;

        public string ConnectionString => AnalysisServicesHelper.BuildConnectionString(ServerName, DatabaseName);
    }
}