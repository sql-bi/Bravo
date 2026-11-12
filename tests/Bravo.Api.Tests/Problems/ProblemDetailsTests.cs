using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bravo.Api.Problems;
using Bravo.Core.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Bravo.Api.Tests.Problems;

/// <summary>
/// Every error answer is a problem details body with a code, whatever produced it.
/// </summary>
public sealed class ProblemDetailsTests
{
    private const string KnownErrorMessage = "The model was changed by another program.";
    private const string UnexpectedMessage = "The secret detail of a defect.";

    [Fact]
    public async Task ErrorThatAUseCaseForesaw_IsAnswered409WithItsCodeAndItsMessage()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync("/test/known", TestContext.Current.CancellationToken);

        var problem = await response.ReadJsonAsync();
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("ModelChanged", problem.GetProperty("code").GetString());
        Assert.Equal(KnownErrorMessage, problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task UnexpectedException_IsAnswered500WithTheGenericCode()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync("/test/unexpected", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(ProblemCodes.Unexpected, await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task UnexpectedException_TellsNothingOfTheException()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync("/test/unexpected", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(UnexpectedMessage, body, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(InvalidOperationException), body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValueThatIsNotAllowed_IsAnswered400WithTheMemberAndTheCodeOfTheValidation()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.GetAsync("/events?after=-1", TestContext.Current.CancellationToken);

        var problem = await response.ReadJsonAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemCodes.ValidationFailed, problem.GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Array, problem.GetProperty("errors").GetProperty("after").ValueKind);
    }

    [Theory]
    [InlineData("/events?after=soon")]
    [InlineData("/events?after=1.5")]
    public async Task ParameterOfTheWrongType_IsAnswered400WithTheCodeOfARequestThatCannotBeRead(string route)
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.GetAsync(route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemCodes.InvalidRequest, await response.ReadProblemCodeAsync());
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("[]")]
    [InlineData("null")]
    public async Task BodyThatIsNotWhatTheRouteExpects_IsAnswered400(string body)
    {
        await using var api = await TestApi.StartAsync();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await api.Client.PutAsync("/options", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemCodes.InvalidRequest, await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task BodyThatIsNotJson_IsAnswered415WithItsCode()
    {
        await using var api = await TestApi.StartAsync();
        using var content = new StringContent("theme=Dark", Encoding.UTF8, "text/plain");

        using var response = await api.Client.PutAsync("/options", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal(ProblemCodes.UnsupportedMediaType, await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task PathThatMatchesNoRoute_IsAnswered404WithItsCode()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.GetAsync("/no-such-route", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemCodes.NotFound, await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task MethodThatTheRouteDoesNotHave_IsAnswered405WithItsCode()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.DeleteAsync("/options", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(ProblemCodes.MethodNotAllowed, await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task StatusWithoutACodeOfItsOwn_GetsACodeFromItsNumber()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync("/test/teapot", TestContext.Current.CancellationToken);

        Assert.Equal("Http418", await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task EveryProblem_HasTheMembersOfTheStandard()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.GetAsync("/no-such-route", TestContext.Current.CancellationToken);

        var problem = await response.ReadJsonAsync();
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", problem.GetProperty("title").GetString());
        Assert.StartsWith("https://", problem.GetProperty("type").GetString(), StringComparison.Ordinal);
    }

    private static void MapFailingRoutes(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/test/known", ThrowKnown);
        routes.MapGet("/test/unexpected", ThrowUnexpected);
        routes.MapGet("/test/teapot", () => Microsoft.AspNetCore.Http.TypedResults.StatusCode(418));
    }

    private static string ThrowKnown()
    {
        throw new BravoException("ModelChanged", KnownErrorMessage);
    }

    private static string ThrowUnexpected()
    {
        throw new InvalidOperationException(UnexpectedMessage);
    }
}
