namespace Sqlbi.Bravo.Infrastructure.Services.BestPracticeAnalyzer
{
    using Newtonsoft.Json;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Extensions.DynamicLinq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using TabularEditor.TOMWrapper;

    internal class BestPracticeRule
    {
        private readonly Dictionary<RuleScope, IQueryable> _queries;
        private bool _invalidCompatibilityLevel = false;
        private bool _needsRecompile = true;
        private string? _expression;
        private RuleScope _errorScope;
        private RuleScope _scope;

        public BestPracticeRule()
        {
            _queries = new();
        }

        public string? ID { get; set; }

        public string? Name { get; set; }

        public string? Category { get; set; }

        [JsonIgnore]
        public bool Enabled { get; set; } = true;

        public string? Description { get; set; }

        public int Severity { get; set; } = 1;

        [JsonConverter(typeof(RuleScopeConverter))]
        public RuleScope Scope
        {
            get
            {
                return _scope;
            }
            set
            {
                if (_scope != value)
                    _needsRecompile = true;

                _scope = value;
            }
        }

        [JsonIgnore]
        public string ScopeString
        {
            get
            {
                var scopeString = Scope switch
                {
                    RuleScope.CalculatedColumn | RuleScope.CalculatedTableColumn | RuleScope.DataColumn => "Columns",
                    _ => string.Join(",", Scope.Enumerate().Select((scope) => scope.GetTypeName())),
                };

                return scopeString;
            }
        }

        public string? Expression
        {
            get
            {
                return _expression;
            }
            set
            {
                if (_expression != value) 
                    _needsRecompile = true;

                _expression = value;
            }
        }

        public string? FixExpression { get; set; }

        public int? CompatibilityLevel { get; set; }

        [JsonIgnore]
        public bool IsValid { get; private set; }

        [JsonIgnore]
        public int ObjectCount { get; private set; }

        [JsonIgnore]
        public bool HasError => !ErrorMessage.IsNullOrEmpty();

        [JsonIgnore]
        public string? ErrorMessage { get; private set; }

        public string? Remarks { get; set; }

        public bool ShouldSerializeCategory() => !Category.IsNullOrEmpty();

        public bool ShouldSerializeDescription() => !Description.IsNullOrEmpty();

        public bool ShouldSerializeFixExpression() => !FixExpression.IsNullOrEmpty();

        public bool ShouldSerializeRemarks() => !Remarks.IsNullOrEmpty();

        public IEnumerable<AnalyzerResult> Analyze(Model model)
        {
            if (!Enabled)
            {
                yield return new AnalyzerResult
                {
                    Rule = this,
                    RuleEnabled = false
                };
                yield break;
            }

            ObjectCount = 0;

            if (_needsRecompile)
            {
                CompileQueries(model);
            }

            if (!IsValid)
            {
                yield return new AnalyzerResult
                {
                    Rule = this,
                    InvalidCompatibilityLevel = _invalidCompatibilityLevel,
                    RuleError = ErrorMessage,
                    RuleErrorScope = _errorScope
                };
                yield break;
            }

            var tabularObjects = new List<ITabularNamedObject>();
            var currentScope = RuleScope.Model;
            try
            {
                foreach (var query in _queries)
                {
                    currentScope = query.Key;
                    tabularObjects.AddRange(query.Value.OfType<ITabularNamedObject>());
                    ObjectCount += tabularObjects.Count;
                }

                IsValid = true;
            }
            catch (Exception ex)
            {
                _errorScope = currentScope;

                ErrorMessage = "Error while evaluating expression on " + currentScope.GetTypeName() + ": " + ex.Message;
                IsValid = false;
            }

            if (!IsValid)
            {
                yield return new AnalyzerResult
                {
                    Rule = this,
                    InvalidCompatibilityLevel = _invalidCompatibilityLevel,
                    RuleError = ErrorMessage,
                    RuleErrorScope = _errorScope
                };
                yield break;
            }

            foreach (var tabularObject in tabularObjects)
            {
                yield return new AnalyzerResult
                {
                    Rule = this,
                    Object = tabularObject
                };
            }
        }

        private void CompileQueries(Model model)
        {
            _queries.Clear();
            ErrorMessage = null;

            if (CompatibilityLevel > model.Database.CompatibilityLevel)
            {
                _invalidCompatibilityLevel = true;
                IsValid = false;
                return;
            }

            try
            {
                foreach (var scope in Scope.Enumerate())
                {
                    _errorScope = scope;
                    var query = CompileQuery(model, scope);
                    _queries.Add(scope, query);
                }
                IsValid = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                IsValid = false;
            }
        }

        private IQueryable CompileQuery(Model model, RuleScope scope)
        {
            BravoUnexpectedException.ThrowIfNull(Expression);

            var collection = GetQueryableCollection(model, scope);
            var lambda = Extensions.DynamicLinq.DynamicExpression.ParseLambda(collection.ElementType, typeof(bool), Expression);

            var typeArguments = new Type[] { collection.ElementType };
            var arguments = new Expression[] { collection.Expression, System.Linq.Expressions.Expression.Quote(lambda) };
            var call = System.Linq.Expressions.Expression.Call(type: typeof(Queryable), methodName: "Where", typeArguments, arguments);

            var query = collection.Provider.CreateQuery(call);
            return query;
        }

        private static IQueryable GetQueryableCollection(Model model, RuleScope scope)
        {
            return scope switch
            {
                RuleScope.KPI => model.AllMeasures.Where((m) => m.KPI != null).Select((m) => m.KPI).AsQueryable(),
                RuleScope.CalculatedColumn => model.AllColumns.OfType<CalculatedColumn>().AsQueryable(),
                RuleScope.CalculatedTable => model.Tables.OfType<CalculatedTable>().AsQueryable(),
                RuleScope.CalculatedTableColumn => model.Tables.OfType<CalculatedTable>().SelectMany((t) => t.Columns).OfType<CalculatedTableColumn>().AsQueryable(),
                RuleScope.Culture => model.Cultures.AsQueryable(),
                RuleScope.DataColumn => model.AllColumns.OfType<DataColumn>().AsQueryable(),
                RuleScope.ProviderDataSource => model.DataSources.OfType<ProviderDataSource>().AsQueryable(),
                RuleScope.StructuredDataSource => model.DataSources.OfType<StructuredDataSource>().AsQueryable(),
                RuleScope.Hierarchy => model.AllHierarchies.AsQueryable(),
                RuleScope.Level => model.AllLevels.AsQueryable(),
                RuleScope.Measure => model.AllMeasures.AsQueryable(),
                RuleScope.Model => Enumerable.Repeat(model, 1).AsQueryable(),
                RuleScope.Partition => model.AllPartitions.AsQueryable(),
                RuleScope.Perspective => model.Perspectives.AsQueryable(),
                RuleScope.Relationship => model.Relationships.OfType<SingleColumnRelationship>().AsQueryable(),
                RuleScope.Table => model.Tables.Where((t) => t is not CalculatedTable && t is not CalculationGroupTable).AsQueryable(),
                RuleScope.ModelRole => model.Roles.AsQueryable(),
                RuleScope.NamedExpression => model.Expressions.AsQueryable(),
                RuleScope.Variation => model.AllColumns.SelectMany((c) => c.Variations).AsQueryable(),
                RuleScope.TablePermission => model.Roles.SelectMany((r) => r.TablePermissions).AsQueryable(),
                RuleScope.CalculationGroup => model.CalculationGroups.AsQueryable(),
                RuleScope.CalculationItem => model.CalculationGroups.SelectMany((cg) => cg.CalculationItems).AsQueryable(),
                RuleScope.ModelRoleMember => model.Roles.SelectMany((r) => r.Members).AsQueryable(),
                _ => Enumerable.Empty<TabularNamedObject>().AsQueryable(),
            };
        }
    }
}
