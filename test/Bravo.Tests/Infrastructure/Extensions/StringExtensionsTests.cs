namespace Bravo.Tests.Infrastructure.Extensions
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Models.FormatDax;
    using Xunit;

    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("", "", DaxLineBreakStyle.None)]
        [InlineData(null, null, DaxLineBreakStyle.None)]
        [InlineData("\n", "", DaxLineBreakStyle.InitialLineBreak)]
        [InlineData("\r\n", "", DaxLineBreakStyle.InitialLineBreak)]
        public void NormalizeDax_EmptyTest(string? expression, string? expectedExpression, DaxLineBreakStyle expectedLineBreakStyle)
        {
            var (actualExpression, actualLineBreakStyle) = expression.NormalizeDax();

            Assert.Equal(expectedExpression, actualExpression);
            Assert.Equal(expectedLineBreakStyle, actualLineBreakStyle);
        }

        [Theory]
        [InlineData("CALCULATE\r\n(\r\n[Amount]\r\n)", "CALCULATE\n(\n[Amount]\n)")]
        public void NormalizeDax_DefaultEolCharacterTest(string expression, string expectedExpression)
        {
            var (actualExpression, actualLineBreakStyle) = expression.NormalizeDax();

            Assert.Equal(expectedExpression, actualExpression);
            Assert.Equal(DaxLineBreakStyle.None, actualLineBreakStyle);
        }

        [Theory]
        [InlineData("CALCULATE([Amount])\n", "CALCULATE([Amount])")]
        [InlineData("CALCULATE([Amount])\n\n", "CALCULATE([Amount])\n\n")]
        [InlineData("CALCULATE([Amount])\n\n\n", "CALCULATE([Amount])\n\n\n")]
        [InlineData("CALCULATE([Amount]) ", "CALCULATE([Amount])")]
        [InlineData("CALCULATE([Amount])  ", "CALCULATE([Amount])  ")]
        [InlineData("CALCULATE([Amount])   ", "CALCULATE([Amount])   ")]
        [InlineData("CALCULATE([Amount])\n ", "CALCULATE([Amount])\n")]
        public void NormalizeDax_EolTrailingCharacterTest(string expression, string expectedExpression)
        {
            var (actualExpression, actualLineBreakStyle) = expression.NormalizeDax();

            Assert.Equal(expectedExpression, actualExpression);
            Assert.Equal(DaxLineBreakStyle.None, actualLineBreakStyle);
        }
    }
}
