namespace Sqlbi.Bravo.Infrastructure.Services.BestPracticeAnalyzer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TabularEditor.TOMWrapper;

    [Flags]
    internal enum RuleScope
    {
        //None = 0x0000,
        Model = 0x0001,
        Table = 0x0002, // Excludes Calculated Tables even though they derive from this type
        Measure = 0x0004,
        Hierarchy = 0x0008,
        Level = 0x0010,
        Relationship = 0x0020,
        Perspective = 0x0040,
        Culture = 0x0080,
        Partition = 0x0100,
        ProviderDataSource = 0x0200,
        DataColumn = 0x0400,
        CalculatedColumn = 0x0800,
        CalculatedTable = 0x1000,
        CalculatedTableColumn = 0x2000,
        KPI = 0x4000,
        StructuredDataSource = 0x8000,
        Variation = 0x10000,
        NamedExpression = 0x20000,
        ModelRole = 0x40000,
        TablePermission = 0x80000,
        CalculationGroup = 0x100000,
        CalculationItem = 0x200000,
        ModelRoleMember = 0x400000
    }

    internal static class RuleScopeExtensions
    {
        public static RuleScope Combine(this IEnumerable<RuleScope> scopes)
        {
            if (scopes.Any())
            {
                var combined = scopes.Aggregate((r1, r2) => r1 | r2);
                return combined;
            }

            return (RuleScope)0;
        }

        public static bool IsMultiple(this RuleScope scope)
        {
            return ((scope & (scope - 1)) != 0);
        }

        public static string GetTypeName(this RuleScope scope)
        {
            if (scope.IsMultiple())
                throw new InvalidOperationException("The provided RuleScope enum value has more than one flag set.");

            var scopeString = scope.ToString().SplitCamelCase();

            if (scope == RuleScope.Hierarchy)
                scopeString = "Hierarchies";
            else if (scope != RuleScope.Model)
                scopeString += "s";

            return scopeString;
        }

        public static Type GetScopeType(this RuleScope scope)
        {
            if (scope.IsMultiple())
                throw new InvalidOperationException("The provided RuleScope enum value has more than one flag set.");

            var scopeType = scope switch
            {
                RuleScope.Model => typeof(Model),
                RuleScope.Table => typeof(Table),
                RuleScope.Measure => typeof(Measure),
                RuleScope.Hierarchy => typeof(Hierarchy),
                RuleScope.Level => typeof(Level),
                RuleScope.Relationship => typeof(SingleColumnRelationship),
                RuleScope.Perspective => typeof(Perspective),
                RuleScope.Culture => typeof(Culture),
                RuleScope.Partition => typeof(Partition),
                RuleScope.ProviderDataSource => typeof(ProviderDataSource),
                RuleScope.StructuredDataSource => typeof(StructuredDataSource),
                RuleScope.DataColumn => typeof(DataColumn),
                RuleScope.CalculatedColumn => typeof(CalculatedColumn),
                RuleScope.CalculatedTable => typeof(CalculatedTable),
                RuleScope.CalculatedTableColumn => typeof(CalculatedTableColumn),
                RuleScope.KPI => typeof(KPI),
                RuleScope.Variation => typeof(Variation),
                RuleScope.NamedExpression => typeof(NamedExpression),
                RuleScope.ModelRole => typeof(ModelRole),
                RuleScope.CalculationGroup => typeof(CalculationGroupTable),
                //case RuleScope.CalculationGroupAttribute: return typeof(CalculationGroupAttribute);
                RuleScope.CalculationItem => typeof(CalculationItem),
                RuleScope.TablePermission => typeof(TablePermission),
                RuleScope.ModelRoleMember => typeof(ModelRoleMember),
                _ => throw new InvalidOperationException($"Unknown scope '{ scope }'"),
            };

            return scopeType;
        }

        public static RuleScope GetScope(string name)
        {
            if (name == "Hierarchies") 
                return RuleScope.Hierarchy;

            if (name == "Model") 
                return RuleScope.Model;

            var value = (name.EndsWith("s") ? name[0..^1] : name).Replace(" ", "");
            var scope = (RuleScope)Enum.Parse(typeof(RuleScope), value);

            return scope;
        }

        /// <summary>
        /// Enumerates the currently set flags on this Enum as individual Enum values.
        /// </summary>
        public static IEnumerable<RuleScope> Enumerate(this RuleScope input)
        {
            foreach (RuleScope scope in Enum.GetValues(input.GetType()))
            {
                if (input.HasFlag(scope))
                    yield return scope;
            }
        }
    }
}
