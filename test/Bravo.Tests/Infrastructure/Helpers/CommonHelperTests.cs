namespace Bravo.Tests.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using System;
    using Xunit;

    public class CommonHelperTests
    {
        [Theory]
        [InlineData("https://www.contoso.com", "https://www.contoso.com/")]
        [InlineData("https://www.CONTOSO.com", "https://www.contoso.com/")]
        public void NormalizeUriString_SimpleTest(string uriString, string expectedUriString)
        {
            var actualUriString = CommonHelper.NormalizeUriString(uriString);
            Assert.Equal(expectedUriString, actualUriString);
        }

        [Fact]
        public void NormalizeUriString_RelativeUriTest()
        {
            var relativeUri = "path/index.htm?key=value";
            Assert.Throws<UriFormatException>(() => CommonHelper.NormalizeUriString(relativeUri));
        }
    }
}
