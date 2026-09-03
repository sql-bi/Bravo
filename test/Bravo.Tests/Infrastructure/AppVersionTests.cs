using Sqlbi.Bravo.Infrastructure;
using Xunit;

namespace Bravo.Tests.Infrastructure;

public class AppVersionTests
{
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
    public void SemanticVersion_HasNoBuildMetadata()
    {
        Assert.DoesNotContain("+", AppVersion.SemanticVersion);
    }

    [Fact]
    public void SemanticVersion_CarriesTheCommitIdOnlyOutsideAPublicRelease()
    {
        // SemanticVersion must be NBGV SemVer2, the value used for artifact names and installer telemetry: outside
        // publicReleaseRefSpec it ends with 'g{commit}', where the commit is the build metadata of the informational
        // version.
        var metadataIndex = AppVersion.InformationalVersion.IndexOf('+');
        Assert.True(metadataIndex >= 0, $"'{AppVersion.InformationalVersion}' has no build metadata.");

        var commitId = AppVersion.InformationalVersion[(metadataIndex + 1)..];
        var isPublicRelease = ThisAssembly.IsPublicRelease;
        var isPrerelease = ThisAssembly.IsPrerelease;

        if (isPublicRelease)
        {
            Assert.DoesNotContain(commitId, AppVersion.SemanticVersion);
        }
        else
        {
            var separator = isPrerelease ? '.' : '-';
            Assert.EndsWith($"{separator}g{commitId}", AppVersion.SemanticVersion);
        }
    }

    [Fact]
    public void InformationalVersion_StartsWithFileVersion()
    {
        Assert.StartsWith(AppVersion.FileVersion, AppVersion.InformationalVersion);
    }
}
