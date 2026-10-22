using System;
using System.Linq;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics;

public class DiagnosticLogExceptionTests
{
    [Fact]
    public void CreateChain_ReturnsNullWithoutAnException()
    {
        Assert.Null(DiagnosticLogException.CreateChain(null));
    }

    [Fact]
    public void CreateChain_FollowsTheInnerExceptionsOutermostFirst()
    {
        var exception = new InvalidOperationException("outer", new TimeoutException("inner"));

        var chain = DiagnosticLogException.CreateChain(exception);

        Assert.NotNull(chain);
        Assert.Equal(["outer", "inner"], chain.Select((e) => e.Message));
        Assert.Equal(typeof(TimeoutException).FullName, chain[1].Type);
    }

    [Fact]
    public void CreateChain_KeepsEveryInnerExceptionOfAnAggregateDepthFirst()
    {
        var first = new InvalidOperationException("first", new TimeoutException("first inner"));
        var exception = new AggregateException("aggregate", first, new ArgumentException("second"));

        var chain = DiagnosticLogException.CreateChain(exception);

        Assert.NotNull(chain);
        Assert.Equal(["aggregate (first) (second)", "first", "first inner", "second"], chain.Select((e) => e.Message));
    }

    [Fact]
    public void CreateChain_KeepsThePropertiesTheExceptionTypeAdds()
    {
        var exception = new ServiceException("sign-in failed") { ErrorCode = "invalid_grant", StatusCode = 400 };

        var chain = DiagnosticLogException.CreateChain(exception);

        Assert.NotNull(chain);
        var details = Assert.Single(chain).Details;
        Assert.NotNull(details);
        Assert.Equal("invalid_grant", details["ErrorCode"]);
        Assert.Equal("400", details["StatusCode"]);
        Assert.False(details.ContainsKey(nameof(Exception.HResult)));
        Assert.False(details.ContainsKey(nameof(ServiceException.Inner)));
    }

    private sealed class ServiceException(string message) : Exception(message)
    {
        public string? ErrorCode { get; init; }
        public int StatusCode { get; init; }
        public Exception? Inner => InnerException;
    }
}
