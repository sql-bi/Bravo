namespace Sqlbi.Bravo.Infrastructure.Services.BestPracticeAnalyzer
{
    using System.Collections.Generic;

    internal interface IRuleDefinition
    {
        bool Internal { get; }

        string? Name { get; }

        IEnumerable<BestPracticeRule> Rules { get; }
    }
}
