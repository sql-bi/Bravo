using Dax.Formatter;
using Dax.Formatter.Models;
using Microsoft.AnalysisServices;
using Microsoft.AnalysisServices.Tabular;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Helpers;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Settings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services
{
    interface IDaxFormatterServiceTabularObject
    {
        Guid Id { get; }

        string Expression { get; }

        string ExpressionFormatted { get; set; }

        List<string> FormatterErrors { get; }

        string FormatterAssignment { get; }
    }

    internal abstract class DaxFormatterServiceTabularObject : IDaxFormatterServiceTabularObject
    {
        public DaxFormatterServiceTabularObject(string expression) => Expression = expression;

        public Guid Id { get; } = Guid.NewGuid();

        public string Expression { get; }

        public string ExpressionFormatted { get; set; }

        public List<string> FormatterErrors { get; } = new List<string>();

        public string FormatterAssignment => $"[{ Id }] :=";

        public string FormatterExpression => $"{ FormatterAssignment }{ Expression }";
    }

    interface IDaxFormatterServiceTabularNamedObject
    {
        string Name { get; }
    }

    internal class DaxFormatterServiceTabularMeasure : DaxFormatterServiceTabularObject, IDaxFormatterServiceTabularNamedObject
    {
        public DaxFormatterServiceTabularMeasure(string name, string tableName, string expression)
            : base(expression)
        {
            Name = name;
            TableName = tableName;
        }

        public string Name { get; }

        public string TableName { get; }
    }

    internal class DaxFormatterService : IDaxFormatterService, IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly IDaxFormatterClient _client;
        private readonly ILogger _logger;
        private readonly Microsoft.AnalysisServices.Tabular.Server _server;
        private Microsoft.AnalysisServices.Tabular.Database _database;
        private bool _disposed;

        public DaxFormatterService(IDaxFormatterClient client, ILogger<DaxFormatterService> logger)
        {
            _client = client;
            _logger = logger;

            _logger.Trace();
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

                    var connectionString = AnalysisServicesHelper.BuildConnectionString(runtimeSummary.ServerName, runtimeSummary.DatabaseName);
                    _server.Connect(connectionString);
                }

                _database = _server.Databases[runtimeSummary.DatabaseName];
                _database.Model.Sync(new SyncOptions { DiscardLocalChanges = true });
                _database.Refresh();

                Measures = _database.Model.Tables.SelectMany((t) => t.Measures).Select((m) => new DaxFormatterServiceTabularMeasure(m.Name, m.Table.Name, m.Expression)).ToList();

                //IEnumerable<CalculatedColumn> CalculatedColumns => _model.Tables.SelectMany((t) => t.Columns.Where((c) => c.Type == ColumnType.Calculated).Cast<CalculatedColumn>());
                //IEnumerable<KPI> KPIs => _model.Tables.SelectMany((t) => t.Measures.Select((m) => m.KPI)).Where((k) => k != null);
                //IEnumerable<DetailRowsDefinition> DefaultDetailRowsDefinitions => _model.Tables.Select((t) => t.DefaultDetailRowsDefinition).Where((d) => d != null);
                //IEnumerable<DetailRowsDefinition> DetailRowsDefinitions => _model.Tables.SelectMany((t) => t.Measures).Select((m) => m.DetailRowsDefinition).Where((d) => d != null);
                //IEnumerable<CalculationItem> CalculationItems => _model.Tables.Where((t) => t.CalculationGroup != null).SelectMany((t) => t.CalculationGroup.CalculationItems.Cast<CalculationItem>());
            }
        }

        public IEnumerable<DaxFormatterServiceTabularMeasure> Measures { get; private set; }

        public async Task<IEnumerable<IDaxFormatterServiceTabularObject>> FormatAsync(IList<IDaxFormatterServiceTabularObject> tabularObjects)
        {
            var request = new DaxFormatterMultipleRequest
            {
                ServerName = _server.Name,
                DatabaseName = _database.Name
            };

            foreach (var tabularObject in tabularObjects)
            {
                if (tabularObject is DaxFormatterServiceTabularMeasure measure)
                {
                    request.Dax.Add(measure.FormatterExpression);
                }
                else
                {
                    throw new InvalidOperationException($"Invalid { nameof(IDaxFormatterServiceTabularObject) } [{ tabularObject }]");
                }
            }

            var response = await _client.FormatAsync(request);

            for (var i = 0; i < response.Count; i++)
            {
                var result = response[i];
                var tabularObject = tabularObjects[i];

                tabularObject.ExpressionFormatted = result.Formatted?.Replace(tabularObject.FormatterAssignment, string.Empty)?.TrimStart('\r', '\n', ' ')?.TrimEnd('\r', '\n', ' ');
                tabularObject.FormatterErrors.AddRange(result.Errors.Select((e) => $"({ e.Line }, { e.Column }) { e.Message }"));
            }

            return tabularObjects;
        }

        public async Task ApplyFormatAsync(IList<IDaxFormatterServiceTabularObject> tabularObjects)
        {
            await Task.Run(ApplyChanges);

            void ApplyChanges()
            {
                foreach (var tabularObject in tabularObjects)
                {
                    if (tabularObject is DaxFormatterServiceTabularMeasure measure)
                    {
                        _database.Model.Tables[measure.TableName].Measures[measure.Name].Expression = measure.ExpressionFormatted;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid { nameof(IDaxFormatterServiceTabularObject) } [{ tabularObject }]");
                    }
                }

                if (_database.Model.HasLocalChanges)
                    _database.Update();

                var operationResult = _database.Model.SaveChanges();
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
