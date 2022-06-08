namespace Bravo.Tests.Infrastructure.Services.BestPracticeAnalyzer
{
    using Sqlbi.Bravo.Infrastructure.Services.BestPracticeAnalyzer;
    using System.IO;
    using System.Linq;
    using TabularEditor.TOMWrapper;
    using Xunit;

    public class AnalyzerTests : IClassFixture<TabularModelFixture>
    {
        private const string BPARulesStandardPath = @"_data\BestPracticeAnalyzer\TabularEditor_BPARules-standard.json";
        private const string BPARulesStandardRE2ResultsPath = @"_data\BestPracticeAnalyzer\TabularEditor_BPARules-standard.AdventureWorks.TE2Results.json";
        private const string BPARulesPowerBIPath = @"_data\BestPracticeAnalyzer\TabularEditor_BPARules-PowerBI.json";
        private const string BPARulesPowerBIRE2ResultsPath = @"_data\BestPracticeAnalyzer\TabularEditor_BPARules-PowerBI.AdventureWorks.TE2Results.json";

        private readonly TabularModelFixture _fixture;

        public AnalyzerTests(TabularModelFixture fixture)
        {
            _fixture = fixture;
        }

        private TabularModelHandler Handler => _fixture.Handler;

        private Model Model => Handler.Model;

        [Fact]
        public void Analyze_SimpleTest()
        {
            var analyzer = new Analyzer(Model);
            var rule = new BestPracticeRule()
            {
                Scope = RuleScope.Measure | RuleScope.Table,
                Expression = "Description.IndexOf(\"TODO\", StringComparison.OrdinalIgnoreCase) >= 0"
            };
            Model.Tables["Currency"].Description = "Table TODO/reminder";
            Model.Tables["Internet Sales"].Measures["Internet Total Sales"].Description = "Measure todo/reminder";

            var results = analyzer.Analyze(rule).ToArray();

            Assert.Equal(2, results.Length);
            Assert.IsType<Table>(results[0].Object);
            Assert.Same(Model.Tables["Currency"], results[0].Object);
            Assert.IsType<Measure>(results[1].Object);
            Assert.Same(Model.Tables["Internet Sales"].Measures["Internet Total Sales"], results[1].Object);
        }

        [Fact]
        public void Analyze_TokenizeTest()
        {
            var analyzer = new Analyzer(Model);
            var rule = new BestPracticeRule()
            {
                Scope = RuleScope.Measure,
                Expression = "Tokenize().Any(Type = DIV)"
            };

            var results = analyzer.Analyze(rule).ToArray();

            Assert.True(results.Length > 0);
            foreach (var result in results)
            {
                Assert.NotNull(result.Object);
                Assert.IsType<Measure>(result.Object);
                Assert.Contains("/", (result.Object as Measure)!.Expression);
            }
        }

        [Fact(Skip = "TOFIX - test broken due a TOM library update")]
        public void Analyze_StandardRulesTest()
        {
            var rules = BestPracticeCollection.CreateFromFile(BPARulesStandardPath);
            var analyzer = new Analyzer(Model, rules);

            var results = analyzer.Analyze();
            var actualText = Newtonsoft.Json.JsonConvert.SerializeObject(results, Newtonsoft.Json.Formatting.Indented);
            var expectedText = File.ReadAllText(BPARulesStandardRE2ResultsPath);

            Assert.Equal(expectedText, actualText);
        }

        [Fact(Skip = "TOFIX - test broken due a TOM library update")]
        public void Analyze_PowerBIRulesTest()
        {
            var rules = BestPracticeCollection.CreateFromFile(BPARulesPowerBIPath);
            var analyzer = new Analyzer(Model, rules);

            var results = analyzer.Analyze();
            var actualText = Newtonsoft.Json.JsonConvert.SerializeObject(results, Newtonsoft.Json.Formatting.Indented);
            var expectedText = File.ReadAllText(BPARulesPowerBIRE2ResultsPath);

            Assert.Equal(expectedText, actualText);
        }
    }
}
