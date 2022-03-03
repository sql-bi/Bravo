namespace Bravo.Tests.Infrastructure.Services.BestPracticeAnalyzer
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Services.BestPracticeAnalyzer;
    using Xunit;

    public class BestPracticeCollectionTests
    {
        private const string BPARulesStandardPath = @"_data\BestPracticeAnalyzer\TabularEditor_BPARules-standard.json";

        [Fact]
        public void CreateFromFile_SimpleTest()
        {
            var collection = BestPracticeCollection.CreateFromFile(BPARulesStandardPath);

            Assert.NotEmpty(collection);
            Assert.Equal(29, collection.Count);
        }

        [Fact]
        public void Contains_SimpleTest()
        {
            var collection = BestPracticeCollection.CreateFromFile(BPARulesStandardPath);
            var actual = collection.Contains("DAX_TODO");

            Assert.True(actual);
        }

        [Fact]
        public void Indexer_SimpleTest()
        {
            var existsingRule = new BestPracticeRule() { ID = "DAX_TODO" };
            var nonExistingRule = new BestPracticeRule() { ID = "NON EXISTING RULE 123456" };

            var collection = BestPracticeCollection.CreateFromFile(BPARulesStandardPath);

            Assert.Null(collection[nonExistingRule.ID]);
            Assert.NotNull(collection[existsingRule.ID]);
        }

        [Fact]
        public void SerializeToJson_SimpleTest()
        {
            var collection = BestPracticeCollection.CreateFromFile(BPARulesStandardPath);
            var collectionJson = collection.SerializeToJson();

            Assert.NotNull(collectionJson);
            Assert.False(collectionJson.IsNullOrEmpty());
        }
    }
}
