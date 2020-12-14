using Microsoft.AnalysisServices;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Client.Http;
using Sqlbi.Bravo.Core.Client.Http.Interfaces;
using Sqlbi.Bravo.Core.Helpers;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services
{
    internal class DaxFormatterService : IDaxFormatterService, IDisposable
    {
        private readonly SemaphoreSlim _initilizeOrRefreshSemaphore = new SemaphoreSlim(1);
        private readonly IDaxFormatterHttpClient _client;
        private readonly IGlobalSettingsProviderService _settings;
        private readonly ILogger _logger;
        private readonly Server _server;
        private DaxFormatterModelManager _manager;
        private bool _disposed;

        public DaxFormatterService(IDaxFormatterHttpClient client, IGlobalSettingsProviderService settings, ILogger<DaxFormatterService> logger)
        {
            _client = client;
            _settings = settings;
            _logger = logger;

            _logger.Trace();
            _server = new Server();
        }

        public async Task InitilizeOrRefreshAsync()
        {
            _logger.Trace();

            await _initilizeOrRefreshSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                await Task.Run(InitilizeOrRefresh).ConfigureAwait(false);
            }
            finally
            {
                _initilizeOrRefreshSemaphore.Release();
            }

            void InitilizeOrRefresh()
            {
                if (_server.Connected == false)
                {
                    var connectionString = _settings.BuildConnectionString();
                    _server.Connect(connectionString);
                }

                var database = _server.Databases[_settings.Runtime.DatabaseName];
                database.Model.Sync(new Microsoft.AnalysisServices.Tabular.SyncOptions { DiscardLocalChanges = true });
                database.Refresh();

                _manager = DaxFormatterModelManager.CreateFrom(database.Model);
            }
        }

        public async Task FormatAsync(DaxFormatterTabularObjectType objectType)
        {
            _logger.Trace();

            await Task.Run(Format).ConfigureAwait(false);

            async Task Format()
            {
                var database = _server.Databases[_settings.Runtime.DatabaseName];
                var requests = _manager.CreateRequests(objectType);
                var responses = _client.FormatAsync(requests);

                await foreach (var response in responses)
                    _manager.UpdateModelFrom(response);

                if (database.Model.HasLocalChanges)
                    database.Update();

                var operationResult = database.Model.SaveChanges();
                if (operationResult.XmlaResults is { ContainsErrors: true })
                {
                    foreach (XmlaResult result in operationResult.XmlaResults)
                    {
                        foreach (XmlaMessage message in result.Messages)
                        {
                            var json = "{ }";

                            if (message is XmlaError error)
                                json = System.Text.Json.JsonSerializer.Serialize(error);
                            else if (message is XmlaWarning warning)
                                json = System.Text.Json.JsonSerializer.Serialize(warning);

                            _logger.Error(LogEvents.DaxFormatterFormatSaveChangesContainsErrors, message: json);
                        }
                    }
                }
            }
        }

        public int Count(DaxFormatterTabularObjectType objectType) => _manager.Count(objectType);

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

        public List<Microsoft.AnalysisServices.Tabular.Measure> GetMeasures()
            => _manager.GetMeasures();
    }
}
