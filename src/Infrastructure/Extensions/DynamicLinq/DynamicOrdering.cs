namespace Sqlbi.Bravo.Infrastructure.Extensions.DynamicLinq
{
    using System.Linq.Expressions;

    internal class DynamicOrdering
    {
        public Expression? Selector;

        public bool Ascending;
    }
}
