using System;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Updates;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Bravo.Core.Tests.Updates;

public sealed class UpdateCheckTests
{
    private static readonly Uri s_download = new("https://github.com/sql-bi/Bravo/releases/download/v1.1.0/Bravo.1.1.0.x64.msi");
    private static readonly Uri s_releaseNotes = new("https://github.com/sql-bi/Bravo/releases/tag/v1.1.0");

    private readonly IReleaseFeed _feed = Substitute.For<IReleaseFeed>();
    private readonly IInstallationReader _installationReader = Substitute.For<IInstallationReader>();
    private readonly ISystemBeep _beep = Substitute.For<ISystemBeep>();

    [Theory]
    [InlineData("1.1.0.188", "1.0.9805.22885", true)]
    [InlineData("1.1.0.188", "1.1.0.187", true)]
    [InlineData("1.1.0.188", "1.1.0.188", false)]
    [InlineData("1.1.0.188", "1.2.0.12", false)]
    public async Task CheckAsync_VersionOfTheFeed_IsNewerOnlyWhenItIsGreaterInItsFourNumbers(
        string feedVersion,
        string fileVersion,
        bool isNewer)
    {
        var check = CreateCheck(fileVersion, includesRuntime: true);
        GivenTheFeedNames(feedVersion);

        var result = await check.CheckAsync(TestContext.Current.CancellationToken);

        Assert.Equal(isNewer, result.IsNewer);
        Assert.Equal(new Version(feedVersion), result.Version);
    }

    [Fact]
    public async Task CheckAsync_NewerRelease_BeepsOnce()
    {
        var check = CreateCheck("1.0.9805.22885", includesRuntime: true);
        GivenTheFeedNames("1.1.0.188");

        await check.CheckAsync(TestContext.Current.CancellationToken);

        _beep.Received(1).Play();
    }

    [Fact]
    public async Task CheckAsync_ReleaseThatIsNotNewer_DoesNotBeep()
    {
        var check = CreateCheck("1.1.0.188", includesRuntime: true);
        GivenTheFeedNames("1.1.0.188");

        await check.CheckAsync(TestContext.Current.CancellationToken);

        _beep.DidNotReceive().Play();
    }

    [Fact]
    public async Task CheckAsync_CopyOfAKind_GivesTheAddressOfItsFileAndTheReleaseNotesOfTheFeed()
    {
        var check = CreateCheck("1.0.9805.22885", includesRuntime: false);
        GivenTheFeedNames("1.1.0.188");
        _installationReader.Read().Returns(Installation.PerUser);

        var result = await check.CheckAsync(TestContext.Current.CancellationToken);

        var expected = "https://github.com/sql-bi/Bravo/releases/download/v1.1.0/Bravo.1.1.0.x64-frameworkdependent-userinstaller.msi";
        Assert.Equal(new Uri(expected), result.DownloadAddress);
        Assert.Equal(s_releaseNotes, result.ReleaseNotesAddress);
    }

    [Fact]
    public async Task CheckAsync_FeedThatFails_FailsWithItsErrorAndDoesNotBeep()
    {
        var check = CreateCheck("1.0.9805.22885", includesRuntime: true);
        var failure = new BravoException(ErrorCode.UpdateCheckFailed, "The update feed cannot be read.");
        _feed.ReadAsync(Arg.Any<CancellationToken>()).ThrowsAsync(failure);

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => check.CheckAsync(TestContext.Current.CancellationToken));

        Assert.Same(failure, exception);
        _beep.DidNotReceive().Play();
    }

    private UpdateCheck CreateCheck(string fileVersion, bool includesRuntime)
    {
        var options = new UpdateCheckOptions
        {
            FeedAddress = new Uri("https://feed.bravo.test/currentversion.json"),
            FileVersion = new Version(fileVersion),
            IncludesRuntime = includesRuntime,
        };

        return new UpdateCheck(_feed, _installationReader, _beep, options);
    }

    private void GivenTheFeedNames(string version)
    {
        var release = new PublishedRelease(new Version(version), s_download, s_releaseNotes);
        _feed.ReadAsync(Arg.Any<CancellationToken>()).Returns(release);
    }
}
