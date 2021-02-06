using Dax.Formatter.Models;
using Microsoft.AnalysisServices.Tabular;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Helpers;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sqlbi.Bravo.Core.Services
{
    internal class DaxFormatterServiceModelManager
    {
        private class ExpressionInfo
        {
            private readonly Action<MetadataObject, string> _update;

            public ExpressionInfo(MetadataObject metadataObject, string expression, Action<MetadataObject, string> update)
            {
                MetadataObject = metadataObject ?? throw new ArgumentNullException(nameof(metadataObject));
                Expression = expression ?? throw new ArgumentNullException(nameof(expression));
                _update = update ?? throw new ArgumentNullException(nameof(update));
            }

            public string Id { get; } = string.Format(ExpressionIdTemplate, Guid.NewGuid().ToString("N"));

            public MetadataObject MetadataObject { get; }

            public string Expression { get; }

            public void Update(string expression) => _update.Invoke(MetadataObject, expression);
        }

        private class ExpressionInfoCollection : IEnumerable<ExpressionInfo>
        {
            private readonly Dictionary<string, ExpressionInfo> _dictionary = new Dictionary<string, ExpressionInfo>();

            public void Add(IEnumerable<ExpressionInfo> expressions)
            {
                foreach (var expression in expressions)
                    Add(expression);
            }

            public void Add(ExpressionInfo expression) => _dictionary.Add(expression.Id, expression);

            public void Clear() => _dictionary.Clear();

            public ExpressionInfo GetFrom(string id) => _dictionary[id];

            public IEnumerator GetEnumerator() => _dictionary.GetEnumerator();

            IEnumerator<ExpressionInfo> IEnumerable<ExpressionInfo>.GetEnumerator()
            {
                foreach (var value in _dictionary.Values)
                    yield return value;
            }
        }

        private const string ExpressionIdTemplate = "MEASURE '_'[{0}] =";

        internal const string ExpressionIdRegexPattern = "(MEASURE '_'\\[[0-9a-f]{32}\\] =)";

        public static DaxFormatterServiceModelManager CreateFrom(Model model)
        {
            var logger = App.ServiceProvider.GetRequiredService<ILogger<DaxFormatterServiceModelManager>>();
            logger.Trace();

            var manager = new DaxFormatterServiceModelManager(model, logger);
            manager.CollectTabularObjects();
            return manager;
        }

        private readonly Dictionary<MetadataObject, DaxFormatterServiceTabularObjectType> _tabularObjects = new Dictionary<MetadataObject, DaxFormatterServiceTabularObjectType>();
        private readonly ExpressionInfoCollection _expressions = new ExpressionInfoCollection();

        private readonly ILogger _logger;
        private readonly Model _model;

        private DaxFormatterServiceModelManager(Model model, ILogger<DaxFormatterServiceModelManager> logger)
        {
            _model = model;
            _logger = logger;

            _logger.Trace();
        }

        private void CollectTabularObjects()
        {
            // DaxFormatterTabularObjectType.Measures
            {
                //builder.Add(model.DefaultMeasure); // TODO: to include ?
                foreach (var measure in _model.Tables.SelectMany((t) => t.Measures))
                    _tabularObjects.Add(measure, DaxFormatterServiceTabularObjectType.Measures);
            }

            // DaxFormatterTabularObjectType.CalculatedColumns
            {
                foreach (var calculatedColumn in _model.Tables.SelectMany((t) => t.Columns.Where((c) => c.Type == ColumnType.Calculated).Cast<CalculatedColumn>()))
                    _tabularObjects.Add(calculatedColumn, DaxFormatterServiceTabularObjectType.CalculatedColumns);
            }

            // DaxFormatterTabularObjectType.KPIs
            {
                foreach (var kpi in _model.Tables.SelectMany((t) => t.Measures.Select((m) => m.KPI)).Where((k) => k != null))
                    _tabularObjects.Add(kpi, DaxFormatterServiceTabularObjectType.KPIs);
            }

            //DaxFormatterTabularObjectType.DetailRowsDefinitions
            {
                foreach (var detailRowsDefinition in _model.Tables.Select((t) => t.DefaultDetailRowsDefinition).Where((d) => d != null))
                    _tabularObjects.Add(detailRowsDefinition, DaxFormatterServiceTabularObjectType.DetailRowsDefinitions);

                foreach (var detailRowsDefinition in _model.Tables.SelectMany((t) => t.Measures).Select((m) => m.DetailRowsDefinition).Where((d) => d != null))
                    _tabularObjects.Add(detailRowsDefinition, DaxFormatterServiceTabularObjectType.DetailRowsDefinitions);
            }

            //DaxFormatterTabularObjectType.CalculationItems
            {                
                foreach (var calculationItem in _model.Tables.Where((t) => t.CalculationGroup != null).SelectMany((t) => t.CalculationGroup.CalculationItems.Cast<CalculationItem>()))
                    _tabularObjects.Add(calculationItem, DaxFormatterServiceTabularObjectType.CalculationItems);
            }
        }

        public int Count(DaxFormatterServiceTabularObjectType objectType)
        {
            var count = 0;

            var values = Enum.GetValues<DaxFormatterServiceTabularObjectType>().Cast<Enum>().Where(objectType.HasFlag);
            foreach (var value in values)
                count += _tabularObjects.Count((o) => value.Equals(o.Value));

            return count;
        }

        public List<Measure> GetMeasures()
            => _tabularObjects.Where((o) => o.Value == DaxFormatterServiceTabularObjectType.Measures).Select(o => o.Key).Cast<Measure>().ToList();

        public IReadOnlyList<DaxFormatterRequest> CreateRequests(DaxFormatterServiceTabularObjectType objectType, RuntimeSummary runtimeSummary)
        {
            _logger.Trace();

            _expressions.Clear();

            if (objectType.HasFlag(DaxFormatterServiceTabularObjectType.Measures))
            {
                var measures = _tabularObjects.Where((o) => DaxFormatterServiceTabularObjectType.Measures.Equals(o.Value)).Select((o) => o.Key).Cast<Measure>();
                var expressions = measures.Select((m) => new ExpressionInfo(metadataObject: m, expression: m.Expression, update: (o, e) => ((Measure)o).Expression = e));
                _expressions.Add(expressions);
            }

            if (objectType.HasFlag(DaxFormatterServiceTabularObjectType.CalculatedColumns))
            {
                var calculatedColumns = _tabularObjects.Where((o) => DaxFormatterServiceTabularObjectType.CalculatedColumns.Equals(o.Value)).Select((o) => o.Key).Cast<CalculatedColumn>();
                var expressions = calculatedColumns.Select((c) => new ExpressionInfo(metadataObject: c, expression: c.Expression, update: (o, e) => ((CalculatedColumn)o).Expression = e));
                _expressions.Add(expressions);
            }

            if (objectType.HasFlag(DaxFormatterServiceTabularObjectType.KPIs))
            {
                var kpis = _tabularObjects.Where((o) => DaxFormatterServiceTabularObjectType.KPIs.Equals(o.Value)).Select((o) => o.Key).Cast<KPI>();

                foreach (var kpi in kpis)
                {
                    var statusExpression = new ExpressionInfo(metadataObject: kpi, expression: kpi.StatusExpression, update: (o, e) => ((KPI)o).StatusExpression = e);
                    var targetExpression = new ExpressionInfo(metadataObject: kpi, expression: kpi.TargetExpression, update: (o, e) => ((KPI)o).TargetExpression = e);
                    var trendExpression = new ExpressionInfo(metadataObject: kpi, expression: kpi.TrendExpression, update: (o, e) => ((KPI)o).TrendExpression = e);

                    _expressions.Add(statusExpression);
                    _expressions.Add(targetExpression);
                    _expressions.Add(trendExpression);
                }
            }

            if (objectType.HasFlag(DaxFormatterServiceTabularObjectType.DetailRowsDefinitions))
            {
                var detailRowsDefinitions = _tabularObjects.Where((o) => DaxFormatterServiceTabularObjectType.DetailRowsDefinitions.Equals(o.Value)).Select((o) => o.Key).Cast<DetailRowsDefinition>();
                var expressions = detailRowsDefinitions.Select((d) => new ExpressionInfo(metadataObject: d, expression: d.Expression, update: (o, e) => ((DetailRowsDefinition)o).Expression = e));
                _expressions.Add(expressions);
            }

            if (objectType.HasFlag(DaxFormatterServiceTabularObjectType.CalculationItems))
            {
                var CalculationItems = _tabularObjects.Where((o) => DaxFormatterServiceTabularObjectType.CalculationItems.Equals(o.Value)).Select((o) => o.Key).Cast<CalculationItem>();
                var expressions = CalculationItems.Select((d) => new ExpressionInfo(metadataObject: d, expression: d.Expression, update: (o, e) => ((CalculationItem)o).Expression = e));
                _expressions.Add(expressions);
            }

            var requests = new List<DaxFormatterRequest>();
            var takenCount = 0;
            do
            {
                var batchLength = 0;
                var batch = _expressions.Skip(takenCount).TakeWhile((i) =>
                {
                    var isFirst = batchLength == 0;
                    var isValid = (batchLength += i.Expression.Length) <= AppConstants.DaxFormatterTextFormatRequestBatchMaxTextLength;
                    return isValid || isFirst;
                })
                .ToArray();

                if (batch.Length == 0)
                    break;

                takenCount += batch.Length;

                _logger.Information(LogEvents.DaxFormatterModelManagerBuildRequestFromModel, "Request expression count '{RequestExpressionCount}'", args: batch.Length);

                var dax = string.Join(string.Empty, batch.Select((o) => $"{ o.Id }{ o.Expression }{ AppConstants.DaxFormatterTextFormatRequestBatchSeparator }"));
                var request = DaxFormatterRequest.CreateFrom(_model, dax, runtimeSummary);
                requests.Add(request);
            }
            while (true);

            return requests;
        }

        public bool UpdateModelFrom(DaxFormatterResponse response)
        {
            _logger.Trace();

            // TODO REQUIREMENTS: define how to handle response errors
            if (response.Errors.Count > 0)
                return false;
            
            var regex = new Regex(ExpressionIdRegexPattern, RegexOptions.None);
            var splits = regex.Split(response.Formatted).Where((i) => !string.Empty.Equals(i)).ToArray();

            var ids = splits.Where((s, i) => i % 2 == 0);
            var expressions = splits.Where((s, i) => i % 2 == 1).Select((s) => s.TrimStart('\r', '\n', ' ').TrimEnd('\r', '\n', ' '));
            var items = ids.Zip(expressions, (k, v) => (Id: k, Expression: v)).ToDictionary((z) => z.Id, (z) => z.Expression);

            foreach (var (id, expression) in items)
                _expressions.GetFrom(id).Update(expression);

            return true;
        }

        public void UpdateMeasure(string identifier, string updatedExpression)
        {
            _expressions.GetFrom(identifier).Update(updatedExpression);
        }
    }
}
