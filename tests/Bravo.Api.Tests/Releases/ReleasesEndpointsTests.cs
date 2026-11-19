using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Updates;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Bravo.Api.Tests.Releases;

public sealed class ReleasesEndpointsTests
{
    private const string Route = "/releases/latest";

    private static readonly Uri s_download = new("https://github.com/sql-bi/Bravo/releases/download/v1.3.0/Bravo.1.3.0.x64.msi");
    private static readonly Uri s_releaseNotes = new("https://github.com/sql-bi/Bravo/releases/tag/v1.3.0");

    [Fact]
    public async Task GetLatestRelease_NewerRelease_ReturnsItsVersionAndTheAddressOfTheFileOfThisCopy()
    {
        await using var api = await TestApi.StartAsync();
        GivenTheFeedNames(api, "1.3.0.25");
        api.InstallationReader.Read().Returns(Installation.PerUser);

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        var expected = """
            {
              "version": "1.3.0",
              "isNewer": true,
              "downloadAddress": "https://github.com/sql-bi/Bravo/releases/download/v1.3.0/Bravo.1.3.0.x64-userinstaller.msi",
              "releaseNotesAddress": "https://github.com/sql-bi/Bravo/releases/tag/v1.3.0"
            }
            """;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(JsonElement.DeepEquals(JsonDocument.Parse(expected).RootElement, await response.ReadJsonAsync()));
    }

    [Fact]
    public async Task GetLatestRelease_NewerRelease_Beeps()
    {
        await using var api = await TestApi.StartAsync();
        GivenTheFeedNames(api, "1.3.0.25");

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        api.Beep.Received(1).Play();
    }

    [Fact]
    public async Task GetLatestRelease_ReleaseOfThisBuild_IsNotNewerAndDoesNotBeep()
    {
        await using var api = await TestApi.StartAsync();
        GivenTheFeedNames(api, UseCasesServiceCollectionExtensions.FileVersion.ToString());

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        var release = await response.ReadJsonAsync();
        Assert.False(release.GetProperty("isNewer").GetBoolean());
        api.Beep.DidNotReceive().Play();
    }

    [Fact]
    public async Task GetLatestRelease_FeedThatCannotBeRead_IsAnswered409WithTheCodeOfTheCheck()
    {
        await using var api = await TestApi.StartAsync();
        var failure = new BravoException(ErrorCode.UpdateCheckFailed, "The update feed cannot be read.");
        api.ReleaseFeed.ReadAsync(Arg.Any<CancellationToken>()).ThrowsAsync(failure);

        using var response = await api.Client.GetAsync(Route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(nameof(ErrorCode.UpdateCheckFailed), await response.ReadProblemCodeAsync());
    }

    private static void GivenTheFeedNames(TestApi api, string version)
    {
        var release = new PublishedRelease(new Version(version), s_download, s_releaseNotes);
        api.ReleaseFeed.ReadAsync(Arg.Any<CancellationToken>()).Returns(release);
    }
}
