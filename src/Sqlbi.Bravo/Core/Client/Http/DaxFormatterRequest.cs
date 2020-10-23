using Microsoft.AnalysisServices.Tabular;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Helpers;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using Sqlbi.Bravo.UI;
using System;

namespace Sqlbi.Bravo.Core.Client.Http
{
    internal class DaxFormatterRequest
    {
        public static DaxFormatterRequest CreateFrom(Model model, string dax)
        {
            var serverType = model.Server.ServerMode.ToString();
            var serverName = model.Server.Name;
            var databaseName = model.Database.Name;

            var settings = App.ServiceProvider.GetRequiredService<IGlobalSettingsProviderService>();
            if (settings.Runtime.IsExecutedAsExternalToolForPowerBIDesktop)
            {
                serverType = "PBI Desktop";

                var dashIndex = settings.Runtime.ParentProcessMainWindowTitle.LastIndexOf(" - ");
                if (dashIndex >= 0)
                {
                    var pbixName = settings.Runtime.ParentProcessMainWindowTitle.Substring(0, dashIndex);
                    serverName = databaseName = pbixName;
                }
                else
                {
                    var logger = App.ServiceProvider.GetRequiredService<ILogger<DaxFormatterRequest>>();
                    logger.Warning(LogEvents.DaxFormatterUnableToRetrievePowerBIDesktopFileNameFromParentProcessMainWindowTitle, "ParentProcessMainWindowTitle '{ParentProcessMainWindowTitle}'", args: settings.Runtime.ParentProcessMainWindowTitle);
                }
            }

            var request = new DaxFormatterRequest
            {
                ServerName = serverName,
                ServerEdition = model.Server.Edition.ToString(),
                ServerType = serverType,
                ServerLocation = model.Server.ServerLocation.ToString(),
                ServerVersion = model.Server.Version,
                ServerMode = model.Server.ServerMode.ToString(),
                DatabaseName = databaseName,
                DatabaseCompatibilityLevel = model.Database.CompatibilityLevel.ToString(),
                Dax = dax // ?? throw new ArgumentNullException(nameof(dax))
            };

            return request;
        }

        private string _serverName;
        private string _databaseName;

        private DaxFormatterRequest()
        {
        }
        
        public string ServerName
        {
            get => _serverName;
            set => _serverName = value.ToHashSHA256();
        }

        public string ServerEdition { get; set; }

        public string ServerType { get; set; }

        public string ServerMode { get; set; }

        public string ServerLocation { get; set; }

        public string ServerVersion { get; set; }
        
        public string DatabaseName
        {
            get => _databaseName;
            set => _databaseName = value.ToHashSHA256();
        }

        public string DatabaseCompatibilityLevel { get; set; }

        public string Dax { get; set; }

        public int? MaxLineLenght { get; set; } = (int)DaxFormatterLineStyle.LongLine;

        public bool? SkipSpaceAfterFunctionName { get; set; } = Convert.ToBoolean((int)DaxFormatterSpacingStyle.BestPractice);

        public char ListSeparator { get; set; }  = ',';

        public char DecimalSeparator { get; set; } = '.';

        public string CallerApp { get; set; } = AppConstants.ApplicationName;

        public string CallerVersion { get; set; } = AppConstants.ApplicationProductVersion;
    }
}
