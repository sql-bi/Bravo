using Sqlbi.Bravo.Infrastructure;
using Xunit;

namespace Bravo.Tests.Infrastructure;

public class AppVersionTests
{
    [Theory]
    [InlineData("1.1.0.11+ed89094be9", "")]
    [InlineData("1.1.0.13-beta.1+57e453666e", "-beta.1")]
    [InlineData("1.1.0.13-beta.1", "-beta.1")]
    [InlineData("1.1.0.0-build.99+74e5ed577e", "-build.99")]
    [InlineData("1.1.0.13", "")]
    public void GetPrereleaseTag_ReadsTheTag(string informationalVersion, string expected)
    {
        var actual = AppVersion.GetPrereleaseTag(informationalVersion);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FileVersion_IsNumericWithFourFields()
    {
        // The update check parses this value and compares all four fields, so a prerelease tag must never
        // reach it and the fourth field must always be there.
        var parsed = System.Version.TryParse(AppVersion.FileVersion, out var version);

        Assert.True(parsed, $"'{AppVersion.FileVersion}' is not a numeric version.");
        Assert.True(version!.Revision >= 0, $"'{AppVersion.FileVersion}' has no fourth field.");
    }

    [Fact]
    public void SemanticVersion_StartsWithTheNumericReleaseVersion()
    {
        var releaseVersion = System.Version.Parse(AppVersion.FileVersion).ToString(3);

        Assert.StartsWith(releaseVersion, AppVersion.SemanticVersion);
    }

    [Fact]
    public void InformationalVersion_StartsWithFileVersion()
    {
        Assert.StartsWith(AppVersion.FileVersion, AppVersion.InformationalVersion);
    }
}
