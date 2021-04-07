using Dax.Formatter;
using Dax.Formatter.Models;
using Microsoft.AnalysisServices;
using Microsoft.AnalysisServices.Tabular;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Client.DaxFormatter;
using Sqlbi.Bravo.Client.DaxFormatter.Interfaces;
using Sqlbi.Bravo.Core.Helpers;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Settings;
using Sqlbi.Bravo.Core.Settings.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services
{
    internal class DaxFormatterService : IDaxFormatterService, IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly IDaxFormatterClient _formatter;
        private readonly ILogger _logger;
        private readonly Microsoft.AnalysisServices.Tabular.Server _server;
        private Microsoft.AnalysisServices.Tabular.Database _database;
        private bool _disposed;

        public DaxFormatterService(ILogger<DaxFormatterService> logger)
        {
            _logger = logger;

            _logger.Trace();
            _formatter = new DaxFormatterClient();
            _server = new Microsoft.AnalysisServices.Tabular.Server();
        }

        public async Task InitilizeOrRefreshAsync(RuntimeSummary runtimeSummary)
        {
            _logger.Trace();

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
                    // This can happen is the message used to launch a tab in an existing app instance is lost or corrupt.
                    if (string.IsNullOrWhiteSpace(runtimeSummary.ServerName) && string.IsNullOrWhiteSpace(runtimeSummary.DatabaseName))
                    {
                        // Add own error message rather than the generic one from AnalysisServices ("A data source must be specified in the connection string.")
                        throw new ConnectionException("Unable to connect to the database.");
                    }

                    _server.Connect(runtimeSummary.ConnectionString);
                }

                _database = _server.Databases[runtimeSummary.DatabaseName];
                _database.Model.Sync(new SyncOptions { DiscardLocalChanges = true });
                _database.Refresh();

                Measures = _database.Model.Tables.SelectMany((t) => t.Measures).Select((m) => new TabularMeasure(m.Name, m.Table.Name, m.Expression)).ToList();

                //IEnumerable<CalculatedColumn> CalculatedColumns => _model.Tables.SelectMany((t) => t.Columns.Where((c) => c.Type == ColumnType.Calculated).Cast<CalculatedColumn>());
                //IEnumerable<KPI> KPIs => _model.Tables.SelectMany((t) => t.Measures.Select((m) => m.KPI)).Where((k) => k != null);
                //IEnumerable<DetailRowsDefinition> DefaultDetailRowsDefinitions => _model.Tables.Select((t) => t.DefaultDetailRowsDefinition).Where((d) => d != null);
                //IEnumerable<DetailRowsDefinition> DetailRowsDefinitions => _model.Tables.SelectMany((t) => t.Measures).Select((m) => m.DetailRowsDefinition).Where((d) => d != null);
                //IEnumerable<CalculationItem> CalculationItems => _model.Tables.Where((t) => t.CalculationGroup != null).SelectMany((t) => t.CalculationGroup.CalculationItems.Cast<CalculationItem>());
            }
        }

        public IEnumerable<TabularMeasure> Measures { get; private set; }

        public async Task<IEnumerable<ITabularObject>> FormatAsync(IList<ITabularObject> tabularObjects, IDaxFormatterSettings settings)
        {
            _logger.Trace();

            var request = new DaxFormatterMultipleRequest
            {
                ServerName = _server.Name,
                DatabaseName = _database.Name,
                MaxLineLength = settings.DaxFormatterLineStyle
            };
            
            foreach (var tabularObject in tabularObjects)
            {
                if (tabularObject is TabularMeasure measure)
                {
                    request.Dax.Add(measure.FormatterExpression);
                }
                else
                {
                    throw new InvalidOperationException($"Invalid { nameof(ITabularObject) } [{ tabularObject }]");
                }
            }

            var response = await _formatter.FormatAsync(request);

            for (var i = 0; i < response.Count; i++)
            {
                var result = response[i];
                var tabularObject = tabularObjects[i];

                tabularObject.ExpressionFormatted = result.Formatted?.Replace(tabularObject.FormatterAssignment, string.Empty)?.TrimStart('\r', '\n', ' ')?.TrimEnd('\r', '\n', ' ');
                tabularObject.FormatterErrors.AddRange(result.Errors.Select((e) => $"({ e.Line }, { e.Column }) { e.Message }"));
            }

            return tabularObjects;
        }

        public async Task ApplyFormatAsync(IList<ITabularObject> tabularObjects)
        {
            _logger.Trace();

            await Task.Run(ApplyChanges);

            void ApplyChanges()
            {
                foreach (var tabularObject in tabularObjects)
                {
                    switch (tabularObject)
                    {
                        case TabularMeasure measure:
                            _database.Model.Tables[measure.TableName].Measures[measure.Name].Expression = measure.ExpressionFormatted;
                            break;
                        default:
                            throw new InvalidOperationException($"Invalid { nameof(ITabularObject) } [{ tabularObject }]");
                    }
                }

                if (_database.Model.HasLocalChanges)
                    _database.Update();

                _ = _database.Model.SaveChanges();
            }
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
                    _semaphore.Dispose();
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