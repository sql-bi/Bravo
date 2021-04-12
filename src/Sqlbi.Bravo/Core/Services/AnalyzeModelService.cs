using Dax.Metadata;
using Dax.Metadata.Extractor;
using Dax.ViewModel;
using Dax.Vpax.Tools;
using Microsoft.AnalysisServices;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services
{
    internal class AnalyzeModelService : IAnalyzeModelService
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly ILogger _logger;
        private VpaModel _vpaModel;

        public AnalyzeModelService(ILogger<AnalyzeModelService> logger)
        {
            _logger = logger;

            _logger.Trace();
        }

        public async Task InitilizeOrRefreshAsync(ConnectionSettings connectionSettings)
        {
            _logger.Trace();

            await _semaphore.WaitAsync();
            try
            {
                if (connectionSettings.ConnectionType == Settings.ConnectionType.VertiPaqAnalyzerFile)
                {
                    await Task.Run(InitilizeOrRefreshFromVertiPaqAnalyzerFile);
                }
                else
                {
                    await Task.Run(InitilizeOrRefresh);
                }
            }
            finally
            {
                _semaphore.Release();
            }

            void InitilizeOrRefresh()
            {
                var daxModel = default(Model);

                using (var server = new Server())
                {
                    server.Connect(connectionSettings.ConnectionString);

                    var database = server.Databases.GetByName(connectionSettings.DatabaseName);
                    daxModel = TomExtractor.GetDaxModel(database.Model, AppConstants.ApplicationName, AppConstants.ApplicationProductVersion);
                }

                using (var connection = new AdomdConnection(connectionSettings.ConnectionString))
                {
                    DmvExtractor.PopulateFromDmv(daxModel, connection, connectionSettings.ServerName, connectionSettings.DatabaseName, AppConstants.ApplicationName, AppConstants.ApplicationProductVersion);
                    StatExtractor.UpdateStatisticsModel(daxModel, connection, AppConstants.AnalyzeModelUpdateStatisticsModelSampleRowCount);
                }

                _vpaModel = new VpaModel(daxModel);
            }

            void InitilizeOrRefreshFromVertiPaqAnalyzerFile()
            {
                var vpaxContent = VpaxTools.ImportVpax(path: connectionSettings.ConnectionString);

                _vpaModel = new VpaModel(vpaxContent.DaxModel); 
            }
        }

        public async Task ExportVertiPaqAnalyzerModel(string path)
        {
            VpaxTools.ExportVpax(path, _vpaModel.Model);

            await Task.CompletedTask;
        }

        public DateTime LastSyncTime => _vpaModel?.Model?.ExtractionDate ?? DateTime.MinValue;

        public (long DatasetSize, int ColumnCount) DatasetSummary => (DatasetSize: _vpaModel.Tables.Sum((t) => t.ColumnsTotalSize), ColumnCount: _vpaModel.Columns.Count());

        public IEnumerable<VpaColumn> UnusedColumns => _vpaModel.Columns.Where((c) => !c.IsReferenced).ToArray();

        public IEnumerable<VpaColumn> AllColumns => _vpaModel?.Columns;

        public IEnumerable<VpaTable> AllTables => _vpaModel?.Tables;
    }
}
