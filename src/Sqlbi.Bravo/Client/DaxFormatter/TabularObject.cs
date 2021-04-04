using Sqlbi.Bravo.Client.DaxFormatter.Interfaces;
using System;
using System.Collections.Generic;

namespace Sqlbi.Bravo.Client.DaxFormatter
{
    internal abstract class TabularObject : ITabularObject
    {
        public TabularObject(string expression) => Expression = expression;

        public Guid Id { get; } = Guid.NewGuid();

        public string Expression { get; }

        public string ExpressionFormatted { get; set; }

        public List<string> FormatterErrors { get; } = new List<string>();

        public string FormatterAssignment => $"[{ Id }] :=";

        public string FormatterExpression => $"{ FormatterAssignment }{ Expression }";
    }
}
