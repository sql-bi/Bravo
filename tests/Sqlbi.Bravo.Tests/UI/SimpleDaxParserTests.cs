using Sqlbi.Bravo.UI.Controls;
using Sqlbi.Bravo.UI.Controls.Parser;
using Xunit;

namespace Sqlbi.Bravo.Tests.UI
{
    public class SimpleDaxParserTests
    {
        [Fact]
        public void SingleWord_Function()
        {
            var actual = SimpleDaxParser.ParseLine("CALCULATE");

            Assert.Single(actual);
            Assert.Equal("CALCULATE", actual[0].Text);
            Assert.Equal(ParsedTextType.Function, actual[0].TextType);
        }

        [Fact]
        public void SingleWordPlusWhiteSpace_Function()
        {
            var actual = SimpleDaxParser.ParseLine("  CALCULATE   ");

            Assert.Equal(3, actual.Count);
            Assert.Equal("  ", actual[0].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[0].TextType);
            Assert.Equal("CALCULATE", actual[1].Text);
            Assert.Equal(ParsedTextType.Function, actual[1].TextType);
            Assert.Equal("   ", actual[2].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[2].TextType);
        }

        [Fact]
        public void SingleWord_Keyword()
        {
            var actual = SimpleDaxParser.ParseLine("EVALUATE");

            Assert.Single(actual);
            Assert.Equal("EVALUATE", actual[0].Text);
            Assert.Equal(ParsedTextType.Keyword, actual[0].TextType);
        }

        [Fact]
        public void SingleWordPlusWhiteSpace_Keyword()
        {
            var actual = SimpleDaxParser.ParseLine("  MEASURE   ");

            Assert.Equal(3, actual.Count);
            Assert.Equal("  ", actual[0].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[0].TextType);
            Assert.Equal("MEASURE", actual[1].Text);
            Assert.Equal(ParsedTextType.Keyword, actual[1].TextType);
            Assert.Equal("   ", actual[2].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[2].TextType);
        }

        [Fact]
        public void SingleWordPlusWhiteSpace_FunctionContainingNonAlphas()
        {
            var actual = SimpleDaxParser.ParseLine("  T.DIST.2T   ");

            Assert.Equal(3, actual.Count);
            Assert.Equal("  ", actual[0].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[0].TextType);
            Assert.Equal("T.DIST.2T", actual[1].Text);
            Assert.Equal(ParsedTextType.Function, actual[1].TextType);
            Assert.Equal("   ", actual[2].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[2].TextType);
        }

        [Fact]
        public void SingleParen()
        {
            var actual = SimpleDaxParser.ParseLine(")");

            Assert.Single(actual);
            Assert.Equal(")", actual[0].Text);
            Assert.Equal(ParsedTextType.Parenthesis, actual[0].TextType);
        }

        [Fact]
        public void SingleFunction_PlusParens()
        {
            var actual = SimpleDaxParser.ParseLine("CALCULATE()");

            Assert.Equal(3, actual.Count);
            Assert.Equal("CALCULATE", actual[0].Text);
            Assert.Equal(ParsedTextType.Function, actual[0].TextType);
            Assert.Equal("(", actual[1].Text);
            Assert.Equal(ParsedTextType.Parenthesis, actual[1].TextType);
            Assert.Equal(")", actual[2].Text);
            Assert.Equal(ParsedTextType.Parenthesis, actual[2].TextType);
        }

        [Fact]
        public void SingleFunction_BetweenOtherWords()
        {
            var actual = SimpleDaxParser.ParseLine("please CALCULATE this");

            Assert.Equal(5, actual.Count);
            Assert.Equal("please", actual[0].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[0].TextType);
            Assert.Equal(" ", actual[1].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[1].TextType);
            Assert.Equal("CALCULATE", actual[2].Text);
            Assert.Equal(ParsedTextType.Function, actual[2].TextType);
            Assert.Equal(" ", actual[3].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[3].TextType);
            Assert.Equal("this", actual[4].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[4].TextType);
        }

        [Fact]
        public void SingleFunction_PlusOtherWords_BetweenParens()
        {
            var actual = SimpleDaxParser.ParseLine("CALCULATE ( this )");

            Assert.Equal(7, actual.Count);
            Assert.Equal("CALCULATE", actual[0].Text);
            Assert.Equal(ParsedTextType.Function, actual[0].TextType);
            Assert.Equal(" ", actual[1].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[1].TextType);
            Assert.Equal("(", actual[2].Text);
            Assert.Equal(ParsedTextType.Parenthesis, actual[2].TextType);
            Assert.Equal(" ", actual[3].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[3].TextType);
            Assert.Equal("this", actual[4].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[4].TextType);
            Assert.Equal(" ", actual[5].Text);
            Assert.Equal(ParsedTextType.PlainText, actual[5].TextType);
            Assert.Equal(")", actual[6].Text);
            Assert.Equal(ParsedTextType.Parenthesis, actual[6].TextType);
        }
    }
}
