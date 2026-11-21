using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Errors;
using Bravo.Core.Network;
using Bravo.Core.Settings;
using Bravo.Platform.Windows.Network;
using Bravo.Platform.Windows.Tests.Updates;
using NSubstitute;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Network;

/// <summary>
/// The requests go to a proxy and to a server of the test, on the loopback address: nothing leaves the machine,
/// and no name is asked to a name server.
/// </summary>
public sealed class NetworkFailureHandlerTests
{
    private static readonly Uri s_plain = new("http://service.bravo.test/models");
    private static readonly Uri s_secure = new("https://service.bravo.test/models");

    private readonly NetworkCredential _credentials = new("test-user", "test-password");

    [Fact]
    public async Task SendAsync_ThroughAProxy_GivesTheAnswer()
    {
        await using var proxy = new TestProxy();
        using var client = CreateClient(proxy.Address, _credentials);

        var answer = await client.GetStringAsync(s_plain, TestContext.Current.CancellationToken);

        Assert.Equal(proxy.Body, answer);
        Assert.Equal([new TestProxyRequest("GET", s_plain.AbsoluteUri, null)], proxy.Requests);
    }

    [Fact]
    public async Task SendAsync_ProxyThatAsksForCredentialsItDoesNotGet_FailsWithProxyAuthenticationRequired()
    {
        await using var proxy = new TestProxy { Challenges = ["Basic realm=\"test\""], Accepts = _credentials };
        using var client = CreateClient(proxy.Address, new NetworkCredential("test-user", "another-password"));

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => client.GetAsync(s_plain, TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.ProxyAuthenticationRequired, exception.Code);
    }

    [Fact]
    public async Task SendAsync_ProxyThatAsksForCredentialsOnATunnel_FailsWithProxyAuthenticationRequired()
    {
        await using var proxy = new TestProxy { Challenges = ["Basic realm=\"test\""] };
        using var client = CreateClient(proxy.Address, WindowsUser());

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => client.GetAsync(s_secure, TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.ProxyAuthenticationRequired, exception.Code);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task SendAsync_ProxyThatAsksAndGetsItsCredentials_AnswersTheSecondRequest()
    {
        await using var proxy = new TestProxy { Challenges = ["Basic realm=\"test\""], Accepts = _credentials };
        using var client = CreateClient(proxy.Address, _credentials);

        var answer = await client.GetStringAsync(s_plain, TestContext.Current.CancellationToken);

        Assert.Equal(proxy.Body, answer);
        Assert.Equal([null, "Basic"], proxy.Requests.Select(request => request.AuthorizationScheme));
    }

    // The Windows user never sends a password: a proxy that asks with Basic gets nothing.
    [Fact]
    public async Task SendAsync_ProxyThatAsksWithBasicAndTheWindowsUser_SendsNoCredentials()
    {
        await using var proxy = new TestProxy { Challenges = ["Basic realm=\"test\""] };
        using var client = CreateClient(proxy.Address, WindowsUser());

        await Assert.ThrowsAsync<BravoException>(() => client.GetAsync(s_plain, TestContext.Current.CancellationToken));

        Assert.Equal([null], proxy.Requests.Select(request => request.AuthorizationScheme));
    }

    // The first message of the exchange is all that a proxy of a test sees: it holds no secret.
    [Fact]
    public async Task SendAsync_ProxyThatAsksWithNegotiate_IsAnsweredAsTheWindowsUser()
    {
        await using var proxy = new TestProxy { Challenges = ["Negotiate", "NTLM"] };
        using var client = CreateClient(proxy.Address, WindowsUser());

        await Assert.ThrowsAsync<BravoException>(() => client.GetAsync(s_plain, TestContext.Current.CancellationToken));

        Assert.Equal([null, "Negotiate"], proxy.Requests.Select(request => request.AuthorizationScheme));
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task SendAsync_ProxyThatRefusesTheTunnel_FailsWithProxyConnectionRefused(HttpStatusCode status)
    {
        await using var proxy = new TestProxy { TunnelStatus = status };
        using var client = CreateClient(proxy.Address, _credentials);

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => client.GetAsync(s_secure, TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.ProxyConnectionRefused, exception.Code);
    }

    [Fact]
    public async Task SendAsync_ProxyThatDoesNotListen_FailsWithServerUnreachable()
    {
        using var client = CreateClient(ClosedAddress(), _credentials);

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => client.GetAsync(s_plain, TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.ServerUnreachable, exception.Code);
        Assert.Equal("The server service.bravo.test cannot be reached.", exception.Message);
    }

    // Any failure of the secure connection has this cause for .NET: a certificate that is not trusted too.
    [Fact]
    public async Task SendAsync_ServerThatDoesNotSpeakTls_FailsWithSecureConnectionFailed()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var answering = AnswerWithoutTlsAsync(listener, TestContext.Current.CancellationToken);
        var address = new Uri($"https://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/");
        using var client = CreateClient(proxy: null, _credentials);

        var exception = await Assert.ThrowsAsync<BravoException>(
            () => client.GetAsync(address, TestContext.Current.CancellationToken));

        Assert.Equal(ErrorCode.SecureConnectionFailed, exception.Code);
        await answering;
    }

    [Fact]
    public async Task SendAsync_AnswerOfTheServerThatIsNotASuccess_IsTheAnswer()
    {
        using var client = CreateClient(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        using var response = await client.GetAsync(s_plain, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_ExceptionThatTellsNoCause_PassesAsItIs()
    {
        var unknown = new HttpRequestException(HttpRequestError.ResponseEnded);
        using var client = CreateClient(_ => throw unknown);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetAsync(s_plain, TestContext.Current.CancellationToken));

        Assert.Same(unknown, exception);
    }

    // The request of the user interface was aborted: nothing failed.
    [Fact]
    public async Task SendAsync_RequestCancelledByTheCaller_IsCancelled()
    {
        using var client = CreateClient(async cancellationToken =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("The delay ends only when it is cancelled.");
        });
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync(s_plain, cancellation.Token));
    }

    // A proxy of the user at an address, with the handler that the application builds for it. No address: no
    // proxy.
    private static HttpClient CreateClient(Uri? proxy, ICredentials credentials)
    {
        var settings = proxy is null
            ? new ProxySettings { Type = ProxyType.None }
            : new ProxySettings { Type = ProxyType.Custom, Address = proxy.AbsoluteUri, BypassOnLocal = false };
        var selection = ProxySelection.For(settings, new ProxySelectionOptions());

        return new HttpClient(selection.CreateHandler(credentials));
    }

    private static HttpClient CreateClient(Func<CancellationToken, Task<HttpResponseMessage>> answer)
    {
        return new HttpClient(new NetworkFailureHandler { InnerHandler = new FeedHandler(answer) });
    }

    // The answer of the application to a proxy while the user has not chosen credentials of their own.
    private static ProxyCredentials WindowsUser()
    {
        var settings = Substitute.For<IUserSettingsStore>();
        settings.Current.Returns(UserSettings.Default);

        return new ProxyCredentials(settings, Substitute.For<IProxyCredentialStore>());
    }

    private static Uri ClosedAddress()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return new Uri($"http://127.0.0.1:{port}/");
    }

    private static async Task AnswerWithoutTlsAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        using var client = await listener.AcceptTcpClientAsync(cancellationToken);
        var answer = Encoding.ASCII.GetBytes("HTTP/1.1 400 Bad Request\r\nConnection: close\r\n\r\n");
        await client.GetStream().WriteAsync(answer, cancellationToken);
    }
}
