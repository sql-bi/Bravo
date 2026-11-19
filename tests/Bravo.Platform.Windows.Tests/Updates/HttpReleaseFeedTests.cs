using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Updates;
using Bravo.Platform.Windows.Updates;
using NSubstitute;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Updates;

public sealed class HttpReleaseFeedTests
{
    private const string Feed = """
        {
          "channel": "Stable",
          "version": "1.0.9805.22885",
          "download": "https://github.com/sql-bi/Bravo/releases/download/v1.0.14/Bravo.1.0.14.x64.msi",
          "changelog": "https://github.com/sql-bi/Bravo/releases"
        }
        """;

    private static readonly Uri s_feedAddress = new("https://feed.bravo.test/bravo-public/currentversion.json");
    private static readonly DateTimeOffset s_now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    // The feed of today: UTF-16 with a byte order mark, and no charset in the headers.
    [Fact]
    public async Task ReadAsync_FeedInUtf16WithByteOrderMark_ReadsTheRelease()
    {
        var feed = CreateFeed(_ => Answer(Encoding.Unicode.GetPreamble(), Encoding.Unicode.GetBytes(Feed)));

        var release = await feed.ReadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new Version(1, 0, 9805, 22885), release.Version);
        Assert.Equal(new Uri("https://github.com/sql-bi/Bravo/releases/download/v1.0.14/Bravo.1.0.14.x64.msi"), release.Download);
        Assert.Equal(new Uri("https://github.com/sql-bi/Bravo/releases"), release.ReleaseNotes);
    }

    // What the release writes: UTF-8 with no byte order mark.
    [Fact]
    public async Task ReadAsync_FeedInUtf8_ReadsTheRelease()
    {
        var feed = CreateFeed(_ => Answer([], Encoding.UTF8.GetBytes(Feed)));

        var release = await feed.ReadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new Version(1, 0, 9805, 22885), release.Version);
    }

    [Fact]
    public async Task ReadAsync_AnyFeed_AsksForItWithTheTimeOfTheRequestSoThatNoCacheAnswers()
    {
        var handler = new FeedHandler(_ => Answer([], Encoding.UTF8.GetBytes(Feed)));
        var feed = CreateFeed(handler, s_feedAddress);

        await feed.ReadAsync(TestContext.Current.CancellationToken);

        var expected = $"{s_feedAddress}?nocache={s_now.ToUnixTimeSeconds()}";
        Assert.Equal(new Uri(expected), handler.RequestedAddress);
    }

    [Fact]
    public async Task ReadAsync_FeedAddressWithAQuery_AddsTheTimeToIt()
    {
        var handler = new FeedHandler(_ => Answer([], Encoding.UTF8.GetBytes(Feed)));
        var feed = CreateFeed(handler, new Uri("http://127.0.0.1:8099/currentversion.json?name=newer"));

        await feed.ReadAsync(TestContext.Current.CancellationToken);

        var expected = $"http://127.0.0.1:8099/currentversion.json?name=newer&nocache={s_now.ToUnixTimeSeconds()}";
        Assert.Equal(new Uri(expected), handler.RequestedAddress);
    }

    [Fact]
    public async Task ReadAsync_NoFeedAddress_FailsWithoutARequest()
    {
        var handler = new FeedHandler(_ => Answer([], Encoding.UTF8.GetBytes(Feed)));
        var feed = CreateFeed(handler, feedAddress: null);

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => feed.ReadAsync(TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.UpdateCheckFailed, exception.Code);
        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.ProxyAuthenticationRequired)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ReadAsync_AnswerThatIsNotASuccess_FailsWithTheCodeOfTheCheck(HttpStatusCode status)
    {
        var feed = CreateFeed(_ => Task.FromResult(new HttpResponseMessage(status)));

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => feed.ReadAsync(TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.UpdateCheckFailed, exception.Code);
    }

    [Fact]
    public async Task ReadAsync_NoConnection_FailsWithTheCodeOfTheCheck()
    {
        var feed = CreateFeed(_ => throw new HttpRequestException("No such host is known."));

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => feed.ReadAsync(TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.UpdateCheckFailed, exception.Code);
    }

    [Fact]
    public async Task ReadAsync_CharsetThatNamesNoEncoding_FailsWithTheCodeOfTheCheck()
    {
        var feed = CreateFeed(_ =>
        {
            var content = new ByteArrayContent(Encoding.UTF8.GetBytes(Feed));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "no-such-charset" };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        });

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => feed.ReadAsync(TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.UpdateCheckFailed, exception.Code);
    }

    [Fact]
    public async Task ReadAsync_NoAnswerWithinTheTimeoutOfTheClient_FailsWithTheCodeOfTheCheck()
    {
        var handler = new FeedHandler(async cancellationToken =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("The delay ends only when it is cancelled.");
        });
        var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(1) };
        var feed = new HttpReleaseFeed(httpClient, CreateOptions(s_feedAddress), CreateTimeProvider());

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => feed.ReadAsync(TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.UpdateCheckFailed, exception.Code);
    }

    // The request of the user interface was aborted: nothing failed, and nobody waits for an answer.
    [Fact]
    public async Task ReadAsync_RequestCancelledByTheCaller_IsCancelled()
    {
        var feed = CreateFeed(async cancellationToken =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("The delay ends only when it is cancelled.");
        });
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => feed.ReadAsync(cancellation.Token));
    }

    private static HttpReleaseFeed CreateFeed(Func<CancellationToken, Task<HttpResponseMessage>> answer)
    {
        return CreateFeed(new FeedHandler(answer), s_feedAddress);
    }

    private static HttpReleaseFeed CreateFeed(FeedHandler handler, Uri? feedAddress)
    {
        return new HttpReleaseFeed(new HttpClient(handler), CreateOptions(feedAddress), CreateTimeProvider());
    }

    private static UpdateCheckOptions CreateOptions(Uri? feedAddress)
    {
        return new UpdateCheckOptions
        {
            FeedAddress = feedAddress,
            FileVersion = new Version(1, 1, 0, 188),
            IncludesRuntime = true,
        };
    }

    private static TimeProvider CreateTimeProvider()
    {
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(s_now);
        return timeProvider;
    }

    // As blob storage answers: JSON with no charset, so that only the byte order mark tells the encoding.
    private static Task<HttpResponseMessage> Answer(byte[] preamble, byte[] body)
    {
        var content = new ByteArrayContent([.. preamble, .. body]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
    }
}
