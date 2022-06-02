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

        [Fact]
        public void AppendApplicationVersion_Test()
        {
            var actual = "Bravo for Power BI".AppendApplicationVersion();

            Assert.NotNull(actual);
            Assert.StartsWith("Bravo for Power BI", actual);
        }

        [Theory]
        [InlineData(null, "", 4)]
        [InlineData("", "", 4)]
        [InlineData("0.0.0.999-DEV", "0.0.0.999-DEV", 4)]
        [InlineData("0.9.4", "0.9.4", 4)]
        [InlineData("0.9.4-internal", "0.9.4-internal", 4)]
        [InlineData("0.9.4-internal-20220531.2-main-cf160fee710b983bbfb739e5eb1edd542a72b8b5 (0.9.8186.35439)", "0.9.4-internal-20220531.2-main", 4)]
        [InlineData("0.9.4-internal-20220531.2-main-cf160fee710b983bbfb739e5eb1edd542a72b8b5 (0.9.8186.35439)", "0.9.4-internal-20220531.2", 3)]
        [InlineData("0.9.4-internal-20220531.2-main-cf160fee710b983bbfb739e5eb1edd542a72b8b5 (0.9.8186.35439)", "0.9.4-internal", 2)]
        [InlineData("0.9.4-internal-20220531.2-main-cf160fee710b983bbfb739e5eb1edd542a72b8b5 (0.9.8186.35439)", "0.9.4", 1)]
        [InlineData("0.9.4-internal-20220531.2-main-cf160fee710b983bbfb739e5eb1edd542a72b8b5 (0.9.8186.35439)", "", 0)]
        [InlineData("0.9.4-internal-20220531.2-main-cf160fee710b983bbfb739e5eb1edd542a72b8b5 (0.9.8186.35439)", "", -1)]
        public void GetVersionParts_Test(string? value, string expected, int parts)
        {
            var actual = value.GetVersionParts(parts);

            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }
    }
}
