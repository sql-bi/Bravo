using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bravo.Api.Problems;
using Xunit;

namespace Bravo.Api.Tests.Security;

/// <summary>
/// A request names the loopback address as its host, or it is refused: a page that reaches the port through
/// a name of its own never gets an answer of the API.
/// </summary>
public sealed class HostFilteringTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.1:54321")]
    public async Task Request_ThatNamesTheLoopbackAddress_IsAnswered(string host)
    {
        await using var api = await TestApi.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/options");
        request.Headers.Host = host;

        using var response = await api.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("bravo.example")]
    [InlineData("127.0.0.1.bravo.example")]
    public async Task Request_ThatNamesAnotherHost_IsAnswered400WithItsCode(string host)
    {
        await using var api = await TestApi.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/options");
        request.Headers.Host = host;

        using var response = await api.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemCodes.InvalidRequest, await response.ReadProblemCodeAsync());
    }
}
