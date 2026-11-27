using Xunit;

namespace Bravo.Setup.Tests
{
    public sealed class FullPathTests
    {
        [Theory]
        [InlineData(@"C:\")]
        [InlineData(@"c:\Program Files\SQLBI\Bravo\Bravo.exe")]
        [InlineData("C:/Program Files/SQLBI/Bravo/Bravo.exe")]
        [InlineData(@"\\server\share\Bravo.exe")]
        [InlineData(@"\\?\C:\Program Files\SQLBI\Bravo\Bravo.exe")]
        [InlineData(@"\\.\C:\Program Files\SQLBI\Bravo\Bravo.exe")]
        public void IsFullyQualified_AFullPath_IsTrue(string path)
        {
            Assert.True(FullPath.IsFullyQualified(path));
        }

        // The last one has characters that no file name has: the rule reads them and does not throw.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(@"\")]
        [InlineData(@"\Program Files")]
        [InlineData("C:")]
        [InlineData("C:Bravo.exe")]
        [InlineData("Bravo.exe")]
        [InlineData(@".\Bravo.exe")]
        [InlineData(@"1:\Bravo.exe")]
        [InlineData("<#!PATH!#>")]
        public void IsFullyQualified_APathThatDependsOnTheCurrentFolder_IsFalse(string path)
        {
            Assert.False(FullPath.IsFullyQualified(path));
        }
    }
}
