using Dax.Metadata;
using Dax.Metadata.Extractor;
using Dax.ViewModel;
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
    internal class AnalyzeModelService : IAnalyzeModelService, IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly ILogger _logger;
        private readonly Server _server;
        private VpaModel _vpaModel;
        private Model _daxModel;
        private bool _disposed;

        public AnalyzeModelService(ILogger<AnalyzeModelService> logger)
        {
            _logger = logger;

            _logger.Trace();
            _server = new Server();
        }

        public async Task InitilizeOrRefreshAsync(RuntimeSummary runtimeSummary)
        {
            _logger.Trace();

            if (runtimeSummary.UsingLocalModelForAnanlysis)
            {
                return;
            }

            await _semaphore.WaitAsync();
            try
            {
                await Task.Run(InitilizeOrRefresh);
            }
            finally
            {
                _semaphore.Release();
            }

            void InitilizeOrRefresh()
            {
                if (_server.Connected == false)
                {
                    _server.Connect(runtimeSummary.ConnectionString);
                }

                var database = _server.Databases[runtimeSummary.DatabaseName];

                _daxModel = TomExtractor.GetDaxModel(database.Model, AppConstants.ApplicationName, AppConstants.ApplicationProductVersion);

                using (var connection = new AdomdConnection(runtimeSummary.ConnectionString))
                {
                    DmvExtractor.PopulateFromDmv(_daxModel, connection, runtimeSummary.ServerName, runtimeSummary.DatabaseName, AppConstants.ApplicationName, AppConstants.ApplicationProductVersion);
                    StatExtractor.UpdateStatisticsModel(_daxModel, connection, AppConstants.AnalyzeModelUpdateStatisticsModelSampleRowCount);
                }

                _vpaModel = new VpaModel(_daxModel);
            }
        }

        public DateTime LastSyncTime => _vpaModel?.Model?.ExtractionDate ?? DateTime.MinValue;

        public (long DatasetSize, int ColumnCount) DatasetSummary => (DatasetSize: _vpaModel.Tables.Sum((t) => t.ColumnsTotalSize), ColumnCount: _vpaModel.Columns.Count());

        public IEnumerable<VpaColumn> UnusedColumns => _vpaModel.Columns.Where((c) => !c.IsReferenced).ToArray();

        public IEnumerable<VpaColumn> AllColumns => _vpaModel?.Columns;

        public IEnumerable<VpaTable> AllTables => _vpaModel?.Tables;

        public Model DaxModel
        {
            get => _daxModel;
            set => _vpaModel = new VpaModel(_daxModel = value);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_server.Connected)
                        _server.Disconnect(endSession: true);

                    _server.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
