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
    private static readonly Uri s_feedAddress = new("https://feed.bravo.test/currentversion.json");
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
    public async Task CheckAsync_VersionOfTheFeed_IsNewerAndBeepsOnlyWhenItIsGreaterInItsFourNumbers(
        string feedVersion,
        string fileVersion,
        bool isNewer)
    {
        var check = CreateCheck(fileVersion, includesRuntime: true);
        GivenTheFeedNames(feedVersion);

        var result = await check.CheckAsync(TestContext.Current.CancellationToken);

        Assert.Equal(isNewer, result.IsNewer);
        Assert.Equal(new Version(feedVersion), result.Version);
        _beep.Received(isNewer ? 1 : 0).Play();
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
        _feed.ReadAsync(s_feedAddress, Arg.Any<CancellationToken>()).ThrowsAsync(failure);

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => check.CheckAsync(TestContext.Current.CancellationToken));

        Assert.Same(failure, exception);
        _beep.DidNotReceive().Play();
    }

    // The beep tells a newer release only to a check that answers.
    [Fact]
    public async Task CheckAsync_NewerReleaseWhoseFileCannotBeNamed_FailsAndDoesNotBeep()
    {
        var check = CreateCheck("1.0.9805.22885", includesRuntime: true);
        GivenTheFeedNames("1.1.0.188");
        _installationReader.Read().Throws(new InvalidOperationException("The registry cannot be read."));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => check.CheckAsync(TestContext.Current.CancellationToken));

        _beep.DidNotReceive().Play();
    }

    // A development build started without --dev-update-feed.
    [Fact]
    public async Task CheckAsync_BuildWithoutAFeed_FailsWithTheCodeOfTheCheckAndReadsNothing()
    {
        var options = new UpdateCheckOptions { FileVersion = new Version(1, 1, 0, 188), IncludesRuntime = true };
        var check = new UpdateCheck(_feed, _installationReader, _beep, options);

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => check.CheckAsync(TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.UpdateCheckFailed, exception.Code);
        _ = _feed.DidNotReceiveWithAnyArgs().ReadAsync(default!, TestContext.Current.CancellationToken);
    }

    private UpdateCheck CreateCheck(string fileVersion, bool includesRuntime)
    {
        var options = new UpdateCheckOptions
        {
            FeedAddress = s_feedAddress,
            FileVersion = new Version(fileVersion),
            IncludesRuntime = includesRuntime,
        };

        return new UpdateCheck(_feed, _installationReader, _beep, options);
    }

    // Only the feed of the options names a release.
    private void GivenTheFeedNames(string version)
    {
        var release = new PublishedRelease(new Version(version), s_download, s_releaseNotes);
        _feed.ReadAsync(s_feedAddress, Arg.Any<CancellationToken>()).Returns(release);
    }
}
