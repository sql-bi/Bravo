using System;
using System.Net;
using System.Net.Http;
using Bravo.Core.Errors;
using Bravo.Core.Network;
using Xunit;

namespace Bravo.Core.Tests.Network;

public sealed class NetworkFailureTests
{
    private static readonly Uri s_destination = new("https://service.bravo.test/models?name=sales");

    [Theory]
    [InlineData(HttpRequestError.NameResolutionError, ErrorCode.ServerUnreachable)]
    [InlineData(HttpRequestError.ConnectionError, ErrorCode.ServerUnreachable)]
    [InlineData(HttpRequestError.SecureConnectionError, ErrorCode.SecureConnectionFailed)]
    [InlineData(HttpRequestError.ProxyTunnelError, ErrorCode.ProxyConnectionRefused)]
    public void TryCreate_ExceptionThatTellsACause_IsTheErrorOfTheCause(HttpRequestError error, ErrorCode expected)
    {
        var exception = new HttpRequestException(error);

        var failure = NetworkFailure.TryCreate(exception, s_destination);

        Assert.NotNull(failure);
        Assert.Equal(expected, failure.Code);
        Assert.Same(exception, failure.InnerException);
    }

    [Fact]
    public void TryCreate_TunnelRefusedWith407_IsProxyAuthenticationRequired()
    {
        var exception = new HttpRequestException(
            HttpRequestError.ProxyTunnelError,
            statusCode: HttpStatusCode.ProxyAuthenticationRequired);

        var failure = NetworkFailure.TryCreate(exception, s_destination);

        Assert.Equal(ErrorCode.ProxyAuthenticationRequired, failure?.Code);
        Assert.Same(exception, failure?.InnerException);
    }

    [Theory]
    [InlineData(HttpRequestError.Unknown)]
    [InlineData(HttpRequestError.HttpProtocolError)]
    [InlineData(HttpRequestError.ExtendedConnectNotSupported)]
    [InlineData(HttpRequestError.VersionNegotiationError)]
    [InlineData(HttpRequestError.UserAuthenticationError)]
    [InlineData(HttpRequestError.InvalidResponse)]
    [InlineData(HttpRequestError.ResponseEnded)]
    [InlineData(HttpRequestError.ConfigurationLimitExceeded)]
    public void TryCreate_ExceptionThatTellsNoCause_IsNoError(HttpRequestError error)
    {
        var exception = new HttpRequestException(error);

        var failure = NetworkFailure.TryCreate(exception, s_destination);

        Assert.Null(failure);
    }

    // The answer of the server to a request that was not a success: the caller decides what it means.
    [Fact]
    public void TryCreate_AnswerOfTheServerThatIsNotASuccess_IsNoError()
    {
        var exception = new HttpRequestException(HttpRequestError.Unknown, statusCode: HttpStatusCode.NotFound);

        var failure = NetworkFailure.TryCreate(exception, s_destination);

        Assert.Null(failure);
    }

    // The message travels to the user interface: the host says where, and nothing more of the address does.
    [Fact]
    public void TryCreate_AnyCause_NamesTheHostAndNothingElseOfTheAddress()
    {
        var exception = new HttpRequestException(HttpRequestError.NameResolutionError);

        var failure = NetworkFailure.TryCreate(exception, s_destination);

        Assert.Equal("The server service.bravo.test cannot be reached.", failure?.Message);
    }

    [Fact]
    public void ProxyAuthenticationRequired_WithoutAnException_HasNoCause()
    {
        var failure = NetworkFailure.ProxyAuthenticationRequired(s_destination);

        Assert.Equal(ErrorCode.ProxyAuthenticationRequired, failure.Code);
        Assert.Null(failure.InnerException);
    }

    [Fact]
    public void TimedOut_AnyDestination_IsServerUnreachableWithTheCancellationAsItsCause()
    {
        var cancellation = new OperationCanceledException();

        var failure = NetworkFailure.TimedOut(s_destination, cancellation);

        Assert.Equal(ErrorCode.ServerUnreachable, failure.Code);
        Assert.Same(cancellation, failure.InnerException);
    }
}
