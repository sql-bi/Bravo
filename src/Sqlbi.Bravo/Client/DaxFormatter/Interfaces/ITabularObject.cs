using System;
using System.Collections.Generic;

namespace Sqlbi.Bravo.Client.DaxFormatter.Interfaces
{
    interface ITabularObject
    {
        Guid Id { get; }

        string Expression { get; }

        string ExpressionFormatted { get; set; }

        List<string> FormatterErrors { get; }

        string FormatterAssignment { get; }
    }
}
