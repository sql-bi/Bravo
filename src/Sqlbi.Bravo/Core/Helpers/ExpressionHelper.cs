using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Sqlbi.Bravo.Core.Helpers
{
    internal static class ExpressionHelper
    {
        public static T GetPropertyValue<T>(this Expression<Func<T>> lambda)
        {
            return lambda.Compile().Invoke();
        }

        public static void SetPropertyValue<T>(this Expression<Func<T>> lambda, T value)
        {
            var member = (MemberExpression)lambda.Body;
            var property = (PropertyInfo)member.Member;
            var @object = Expression.Lambda(member.Expression).Compile().DynamicInvoke();
            property.SetValue(@object, value);
        }
    }
}
