namespace Sqlbi.Bravo.Infrastructure.Extensions.DynamicLinq
{
    using Sqlbi.Bravo.Infrastructure.Extensions.DynamicLinq.Parser;
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    internal static class DynamicExpression
    {
        public static Expression Parse(ParameterExpression[]? parameters, Type resultType, string expression, params object[]? values)
        {
            var parser = new ExpressionParser(parameters, expression, values);
            return parser.Parse(resultType);
        }

        public static Expression Parse(Type resultType, string expression, params object[]? values)
        {
            var parser = new ExpressionParser(null, expression, values);
            return parser.Parse(resultType);
        }

        public static LambdaExpression ParseLambda(Type itType, Type resultType, string expression, params object[]? values)
        {
            var parameters = new ParameterExpression[] { Expression.Parameter(itType, "") };
            return ParseLambda(parameters, resultType, expression, values);
        }

        public static LambdaExpression ParseLambda(ParameterExpression[]? parameters, Type resultType, string expression, params object[]? values)
        {
            var parser = new ExpressionParser(parameters, expression, values);
            return Expression.Lambda(parser.Parse(resultType), parameters);
        }

        public static LambdaExpression ParseLambda(Type delegateType, ParameterExpression[]? parameters, Type resultType, string expression, params object[]? values)
        {
            var parser = new ExpressionParser(parameters, expression, values);
            return Expression.Lambda(delegateType, parser.Parse(resultType), parameters);
        }

        public static Expression<Func<T, S>> ParseLambda<T, S>(string expression, params object[] values)
        {
            return (Expression<Func<T, S>>)ParseLambda(typeof(T), typeof(S), expression, values);
        }

        public static Type CreateClass(params DynamicProperty[] properties)
        {
            return ClassFactory.Instance.GetDynamicClass(properties);
        }

        public static Type CreateClass(IEnumerable<DynamicProperty> properties)
        {
            return ClassFactory.Instance.GetDynamicClass(properties);
        }
    }
}
