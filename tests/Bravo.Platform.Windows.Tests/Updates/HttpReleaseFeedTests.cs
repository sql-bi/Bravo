using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Platform.Windows.Updates;
using Microsoft.Extensions.Time.Testing;
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

    private static readonly Func<CancellationToken, Task<HttpResponseMessage>> s_answerInUtf8 =
        _ => Answer([], Encoding.UTF8.GetBytes(Feed));

    private static readonly Func<CancellationToken, Task<HttpResponseMessage>> s_neverAnswer = async cancellationToken =>
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        throw new InvalidOperationException("The delay ends only when it is cancelled.");
    };

    // The feed of today: UTF-16 with a byte order mark, and no charset in the headers.
    [Fact]
    public async Task ReadAsync_FeedInUtf16WithByteOrderMark_ReadsTheRelease()
    {
        var feed = CreateFeed(_ => Answer(Encoding.Unicode.GetPreamble(), Encoding.Unicode.GetBytes(Feed)));

        var release = await feed.ReadAsync(s_feedAddress, TestContext.Current.CancellationToken);

        Assert.Equal(new Version(1, 0, 9805, 22885), release.Version);
        Assert.Equal(new Uri("https://github.com/sql-bi/Bravo/releases/download/v1.0.14/Bravo.1.0.14.x64.msi"), release.Download);
        Assert.Equal(new Uri("https://github.com/sql-bi/Bravo/releases"), release.ReleaseNotes);
    }

    // What the release writes: UTF-8 with no byte order mark.
    [Fact]
    public async Task ReadAsync_FeedInUtf8_ReadsTheRelease()
    {
        var feed = CreateFeed(s_answerInUtf8);

        var release = await feed.ReadAsync(s_feedAddress, TestContext.Current.CancellationToken);

        Assert.Equal(new Version(1, 0, 9805, 22885), release.Version);
    }

    [Fact]
    public async Task ReadAsync_AnyFeed_AsksForItWithTheTimeOfTheRequestSoThatNoCacheAnswers()
    {
        var handler = new FeedHandler(s_answerInUtf8);
        var feed = CreateFeed(handler);

        await feed.ReadAsync(s_feedAddress, TestContext.Current.CancellationToken);

        var expected = $"{s_feedAddress}?nocache={s_now.ToUnixTimeSeconds()}";
        Assert.Equal(new Uri(expected), handler.RequestedAddress);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ReadAsync_AnswerThatIsNotASuccess_FailsWithTheCodeOfTheCheck(HttpStatusCode status)
    {
        var feed = CreateFeed(_ => Task.FromResult(new HttpResponseMessage(status)));

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => feed.ReadAsync(s_feedAddress, TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.UpdateCheckFailed, exception.Code);
    }

    [Fact]
    public async Task ReadAsync_NoConnection_FailsWithTheCodeOfTheCheck()
    {
        var feed = CreateFeed(_ => throw new HttpRequestException("No such host is known."));

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => feed.ReadAsync(s_feedAddress, TestContext.Current.CancellationToken));

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
            () => feed.ReadAsync(s_feedAddress, TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.UpdateCheckFailed, exception.Code);
    }

    [Fact]
    public async Task ReadAsync_NoAnswerWithinTheTimeoutOfTheClient_FailsAsAServerThatCannotBeReached()
    {
        var httpClient = new HttpClient(new FeedHandler(s_neverAnswer)) { Timeout = TimeSpan.FromMilliseconds(1) };
        var feed = new HttpReleaseFeed(httpClient, new FakeTimeProvider(s_now));

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => feed.ReadAsync(s_feedAddress, TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.ServerUnreachable, exception.Code);
    }

    // The client of the application names the cause of a request that did not reach the feed: the check
    // reports that cause, and not that it failed.
    [Fact]
    public async Task ReadAsync_RequestThatFailedWithItsCause_FailsWithThatCause()
    {
        var cause = new BravoException(ErrorCode.ProxyAuthenticationRequired, "The proxy server asks for credentials.");
        var feed = CreateFeed(_ => throw cause);

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => feed.ReadAsync(s_feedAddress, TestContext.Current.CancellationToken));

        Assert.Same(cause, exception);
    }

    // The request of the user interface was aborted: nothing failed, and nobody waits for an answer.
    [Fact]
    public async Task ReadAsync_RequestCancelledByTheCaller_IsCancelled()
    {
        var feed = CreateFeed(s_neverAnswer);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => feed.ReadAsync(s_feedAddress, cancellation.Token));
    }

    private static HttpReleaseFeed CreateFeed(Func<CancellationToken, Task<HttpResponseMessage>> answer)
    {
        return CreateFeed(new FeedHandler(answer));
    }

    private static HttpReleaseFeed CreateFeed(FeedHandler handler)
    {
        return new HttpReleaseFeed(new HttpClient(handler), new FakeTimeProvider(s_now));
    }

    // As blob storage answers: JSON with no charset, so that only the byte order mark tells the encoding.
    private static Task<HttpResponseMessage> Answer(byte[] preamble, byte[] body)
    {
        var content = new ByteArrayContent([.. preamble, .. body]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
    }
}
