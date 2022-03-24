namespace Sqlbi.Bravo.Infrastructure.Extensions.DynamicLinq
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

#nullable disable

    /// <summary>
    /// Microsoft provided class. It allows dynamic string based querying. 
    /// Very handy when, at compile time, you don't know the type of queries that will be generated.
    /// </summary>
    internal static class DynamicQueryable
    {
        #region IQueryable Extensions

        public static HashSet<string> SupportedAggregates = new()
        {
            "Aggregate",
            "All",
            "Any",
            "Average",
            "Contains",
            "Count",
            "Distinct",
            "GroupBy",
            "Max",
            "Min",
            "OrderBy",
            "Select",
            "Skip",
            "Sum",
            "Take",
            "Where",
        };

        public static IQueryable<T> Where<T>(this IQueryable<T> source, string predicate, params object[] values)
        {
            return (IQueryable<T>)Where((IQueryable)source, predicate, values);
        }

        public static IQueryable Where(this IQueryable source, string predicate, params object[] values)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            _ = predicate ?? throw new ArgumentNullException(nameof(predicate));

            LambdaExpression lambda = DynamicExpression.ParseLambda(source.ElementType, typeof(bool), predicate, values);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Where",
                    new Type[] { source.ElementType },
                    source.Expression, Expression.Quote(lambda)));
        }

        public static IQueryable Select(this IQueryable source, string selector, params object[] values)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            _ = selector ?? throw new ArgumentNullException(nameof(selector));

            LambdaExpression lambda = DynamicExpression.ParseLambda(source.ElementType, null, selector, values);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Select",
                    new Type[] { source.ElementType, lambda.Body.Type },
                    source.Expression, Expression.Quote(lambda)));
        }

        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string ordering, params object[] values)
        {
            return (IQueryable<T>)OrderBy((IQueryable)source, ordering, values);
        }

        public static IQueryable OrderBy(this IQueryable source, string ordering, params object[] values)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            _ = ordering ?? throw new ArgumentNullException(nameof(ordering));

            var parameters = new ParameterExpression[] 
            {
                Expression.Parameter(source.ElementType, "") 
            };
            var parser = new DynamicLinq.Parser.ExpressionParser(parameters, ordering, values);
            IEnumerable<DynamicOrdering> orderings = parser.ParseOrdering();
            Expression queryExpr = source.Expression;
            string methodAsc = "OrderBy";
            string methodDesc = "OrderByDescending";
            foreach (DynamicOrdering o in orderings)
            {
                queryExpr = Expression.Call(
                    typeof(Queryable), o.Ascending ? methodAsc : methodDesc,
                    new Type[] { source.ElementType, o.Selector.Type },
                    queryExpr, Expression.Quote(Expression.Lambda(o.Selector, parameters)));
                methodAsc = "ThenBy";
                methodDesc = "ThenByDescending";
            }
            return source.Provider.CreateQuery(queryExpr);
        }

        public static IQueryable Take(this IQueryable source, int count)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Take",
                    new Type[] { source.ElementType },
                    source.Expression, Expression.Constant(count)));
        }

        public static IQueryable Skip(this IQueryable source, int count)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Skip",
                    new Type[] { source.ElementType },
                    source.Expression, Expression.Constant(count)));
        }

        public static IQueryable GroupBy(this IQueryable source, string keySelector, string elementSelector, params object[] values)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            _ = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _ = elementSelector ?? throw new ArgumentNullException(nameof(elementSelector));

            LambdaExpression keyLambda = DynamicExpression.ParseLambda(source.ElementType, null, keySelector, values);
            LambdaExpression elementLambda = DynamicExpression.ParseLambda(source.ElementType, null, elementSelector, values);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "GroupBy",
                    new Type[] { source.ElementType, keyLambda.Body.Type, elementLambda.Body.Type },
                    source.Expression, Expression.Quote(keyLambda), Expression.Quote(elementLambda)));
        }

        public static bool Any(this IQueryable source)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            return (bool)source.Provider.Execute(
                Expression.Call(
                    typeof(Queryable), "Any",
                    new Type[] { source.ElementType }, source.Expression));
        }

        public static int Count(this IQueryable source)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            return (int)source.Provider.Execute(
                Expression.Call(
                    typeof(Queryable), "Count",
                    new Type[] { source.ElementType }, source.Expression));
        }

        public static IQueryable Distinct(this IQueryable source)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Distinct",
                    new Type[] { source.ElementType },
                    source.Expression));
        }

        /// <summary>
        /// Dynamically runs an aggregate function on the IQueryable.
        /// </summary>
        /// <param name="source">The IQueryable data source.</param>
        /// <param name="function">The name of the function to run. Can be Sum, Average, Min, Max.</param>
        /// <param name="member">The name of the property to aggregate over.</param>
        /// <returns>The value of the aggregate function run over the specified property.</returns>
        public static object Aggregate(this IQueryable source, string function, string member)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            _ = member ?? throw new ArgumentNullException(nameof(member));

            // Properties
            var property = source.ElementType.GetProperty(member);
            var parameter = Expression.Parameter(source.ElementType, "s");
            var body = Expression.MakeMemberAccess(parameter, property);
            var selector = Expression.Lambda(body, parameter);
            // We've tried to find an expression of the type Expression<Func<TSource, TAcc>>,
            // which is expressed as ( (TSource s) => s.Price );

            var methods = typeof(Queryable).GetMethods().Where((method) => method.Name == function);

            // Method
            var aggregateMethod = typeof(Queryable).GetMethods().SingleOrDefault(
                m => m.Name == function
                    && m.ReturnType == property.PropertyType // should match the type of the property
                    && m.IsGenericMethod);

            // Sum, Average
            if (aggregateMethod != null)
            {
                return source.Provider.Execute(
                    Expression.Call(
                        null,
                        aggregateMethod.MakeGenericMethod(new[] { source.ElementType }),
                        new[] { source.Expression, Expression.Quote(selector) }));
            }
            // Min, Max
            else
            {
                aggregateMethod = typeof(Queryable).GetMethods().SingleOrDefault(
                    m => m.Name == function
                        && m.GetGenericArguments().Length == 2
                        && m.IsGenericMethod);

                return source.Provider.Execute(
                    Expression.Call(
                        null,
                        aggregateMethod.MakeGenericMethod(new[] { source.ElementType, property.PropertyType }),
                        new[] { source.Expression, Expression.Quote(selector) }));
            }
        }

        #endregion

        #region IEnumerable Extensions

        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, string predicate, params object[] values)
        {
            _= source ?? throw new ArgumentNullException(nameof(source));
            return source.AsQueryable().Where(predicate, values);
        }

        public static IEnumerable Where(this IEnumerable source, string predicate, params object[] values)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            return source.AsQueryable().Where(predicate, values);
        }

        public static IEnumerable Select(this IEnumerable source, string selector, params object[] values)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            return source.AsQueryable().Select(selector, values);
        }

        public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> source, string ordering, params object[] values)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            return source.AsQueryable().OrderBy(ordering, values);
        }

        public static IEnumerable OrderBy(this IEnumerable source, string ordering, params object[] values)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            return source.AsQueryable().OrderBy(ordering, values);
        }

        public static IEnumerable Take(this IEnumerable source, int count)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            return source.AsQueryable().Take(count);
        }

        public static IEnumerable Skip(this IEnumerable source, int count)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            return source.AsQueryable().Skip(count);
        }

        public static IEnumerable GroupBy(this IEnumerable source, string keySelector, string elementSelector, params object[] values)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            return source.AsQueryable().GroupBy(keySelector, elementSelector, values);
        }

        public static bool Any(this IEnumerable source)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            return source.AsQueryable().Any();
        }

        public static int Count(this IEnumerable source)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            return source.AsQueryable().Count();
        }

        public static IEnumerable Distinct(this IEnumerable source)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            return source.AsQueryable().Distinct();
        }

        #endregion
    }
}