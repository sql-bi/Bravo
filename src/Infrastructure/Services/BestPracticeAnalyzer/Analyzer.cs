namespace Sqlbi.Bravo.Infrastructure.Services.BestPracticeAnalyzer
{
    using System.Collections.Generic;
    using System.Linq;
    using TabularEditor.TOMWrapper;

    internal class Analyzer
    {
        private readonly BestPracticeCollection _rules;
        private readonly Model _model;

        public Analyzer(Model model)
            : this(model, BestPracticeCollection.Empty)
        {
        }

        public Analyzer(Model model, BestPracticeCollection rules)
        {
            _model = model;
            _rules = rules;
        }

        public Model Model => _model;

        public BestPracticeCollection Rules => _rules;

        public IEnumerable<AnalyzerResult> Analyze()
        {
            var results = Analyze(Rules);
            return results;
        }

        public IEnumerable<AnalyzerResult> Analyze(BestPracticeRule rule)
        {
            var results = rule.Analyze(Model);
            return results;
        }

        public IEnumerable<AnalyzerResult> Analyze(IEnumerable<BestPracticeRule> rules)
        {
            var results = new List<AnalyzerResult>();

            foreach (var rule in rules)
            {
                var ruleResults = rule.Analyze(Model);
                results.AddRange(ruleResults);
            }

            return results;
        }
    }
}