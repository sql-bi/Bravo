using Sqlbi.Bravo.Client.DaxFormatter.Interfaces;

namespace Sqlbi.Bravo.Client.DaxFormatter
{
    internal class TabularMeasure : TabularObject, ITabularNamedObject
    {
        public TabularMeasure(string name, string tableName, string expression)
            : base(expression)
        {
            Name = name;
            TableName = tableName;
        }

        public string Name { get; }

        public string TableName { get; }
    }
}
