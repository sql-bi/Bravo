using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bravo.Api.Problems;
using Bravo.Core.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Bravo.Api.Tests.Problems;

/// <summary>
/// Every error answer is a problem details body with a code of the list, whatever produced it.
/// </summary>
public sealed class ProblemDetailsTests
{
    private const string KnownErrorMessage = "The model was changed by another program.";
    private const string UnexpectedMessage = "The secret detail of a defect.";
    private const string CauseMessage = "The cause that the answer does not tell.";

    /// <summary>
    /// Every code that a use case can report.
    /// </summary>
    public static TheoryData<ErrorCode> ErrorCodes => [.. Enum.GetValues<ErrorCode>()];

    [Theory]
    [MemberData(nameof(ErrorCodes))]
    public async Task ErrorThatAUseCaseForesaw_IsAnswered409WithItsCodeAndItsMessage(ErrorCode code)
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync($"/test/known/{code}", TestContext.Current.CancellationToken);

        var problem = await response.ReadJsonAsync();
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(code.ToString(), problem.GetProperty("code").GetString());
        Assert.Equal(KnownErrorMessage, problem.GetProperty("detail").GetString());
    }

    // The answer tells the code alone: support finds the cause in the record of the same trace.
    [Fact]
    public async Task ErrorThatAUseCaseForesaw_IsRecordedAsAWarningWithItsCause()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync("/test/known-with-cause", TestContext.Current.CancellationToken);

        var record = Assert.Single(api.Logs.Records, record => record.Category == typeof(BravoExceptionHandler).FullName);
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Equal(CauseMessage, record.Exception?.InnerException?.Message);
    }

    // The handler answered it: the middleware of ASP.NET Core does not record it again, as an error.
    [Fact]
    public async Task ErrorThatAUseCaseForesaw_IsRecordedAsNoError()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync("/test/known-with-cause", TestContext.Current.CancellationToken);

        Assert.DoesNotContain(api.Logs.Records, record => record.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task UnexpectedException_IsRecordedAsAnErrorWithTheException()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync("/test/unexpected", TestContext.Current.CancellationToken);

        var record = Assert.Single(api.Logs.Records, record => record.Exception is not null);
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.Equal(UnexpectedMessage, record.Exception?.Message);
    }

    [Fact]
    public async Task UnexpectedException_IsSentToTheTelemetryOnce()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync("/test/unexpected", TestContext.Current.CancellationToken);

        api.ExceptionTelemetry.Received(1).Track(Arg.Is<Exception>(exception => exception.Message == UnexpectedMessage));
    }

    // The handler of the exceptions cannot answer once the answer has begun: the server ends the connection. No
    // route of the API begins its answer before it ends, so the log alone keeps such an exception.
    [Fact]
    public async Task UnexpectedExceptionAfterTheAnswerBegan_IsRecordedAsAnErrorAndNotSent()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        await Assert.ThrowsAnyAsync<Exception>(() => api.Client.GetStringAsync("/test/unexpected-after-start", TestContext.Current.CancellationToken));

        Assert.Contains(api.Logs.Records, record => record.Level == LogLevel.Error && record.Exception?.Message == UnexpectedMessage);
        api.ExceptionTelemetry.DidNotReceiveWithAnyArgs().Track(default!);
    }

    // The application foresaw it, and the answer says why: no defect to report.
    [Fact]
    public async Task ErrorThatAUseCaseForesaw_IsNotSentToTheTelemetry()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync("/test/known-with-cause", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        api.ExceptionTelemetry.DidNotReceiveWithAnyArgs().Track(default!);
    }

    [Fact]
    public async Task UnexpectedException_IsAnswered500WithTheGenericCode()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync("/test/unexpected", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(nameof(ApiProblemCode.Unexpected), await response.ReadProblemCodeAsync());
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
        Assert.Equal(nameof(ApiProblemCode.ValidationFailed), problem.GetProperty("code").GetString());
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
        Assert.Equal(nameof(ApiProblemCode.InvalidRequest), await response.ReadProblemCodeAsync());
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
        Assert.Equal(nameof(ApiProblemCode.InvalidRequest), await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task BodyThatIsNotJson_IsAnswered415WithItsCode()
    {
        await using var api = await TestApi.StartAsync();
        using var content = new StringContent("theme=Dark", Encoding.UTF8, "text/plain");

        using var response = await api.Client.PutAsync("/options", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal(nameof(ApiProblemCode.UnsupportedMediaType), await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task PathThatMatchesNoRoute_IsAnswered404WithItsCode()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.GetAsync("/no-such-route", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(nameof(ApiProblemCode.NotFound), await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task MethodThatTheRouteDoesNotHave_IsAnswered405WithItsCode()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.DeleteAsync("/options", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(nameof(ApiProblemCode.MethodNotAllowed), await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task StatusWithoutACodeOfItsOwn_GetsTheCodeOfEveryOtherStatus()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync("/test/teapot", TestContext.Current.CancellationToken);

        Assert.Equal(nameof(ApiProblemCode.HttpError), await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task ProblemOfARouteWithACodeOfTheList_KeepsIt()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync("/test/listed", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(nameof(ApiProblemCode.NotFound), await response.ReadProblemCodeAsync());
    }

    // The list is closed: the user interface has a text for each of its codes, and for no other.
    [Fact]
    public async Task ProblemOfARouteWithACodeThatTheListDoesNotHave_GetsTheCodeOfItsStatus()
    {
        await using var api = await TestApi.StartAsync(MapFailingRoutes);

        using var response = await api.Client.GetAsync("/test/unlisted", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(nameof(ApiProblemCode.HttpError), await response.ReadProblemCodeAsync());
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
        routes.MapGet("/test/known/{code}", ThrowKnown);
        routes.MapGet("/test/known-with-cause", ThrowKnownWithCause);
        routes.MapGet("/test/unexpected", ThrowUnexpected);
        routes.MapGet("/test/unexpected-after-start", ThrowAfterTheAnswerBeganAsync);
        routes.MapGet("/test/teapot", () => TypedResults.StatusCode(418));
        routes.MapGet("/test/listed", () => ConflictWithCode(nameof(ApiProblemCode.NotFound)));
        routes.MapGet("/test/unlisted", () => ConflictWithCode("NoSuchCode"));
    }

    private static string ThrowKnown(ErrorCode code)
    {
        throw new BravoException(code, KnownErrorMessage);
    }

    private static string ThrowKnownWithCause()
    {
        throw new BravoException(ErrorCode.UpdateCheckFailed, KnownErrorMessage, new IOException(CauseMessage));
    }

    private static ProblemHttpResult ConflictWithCode(string code)
    {
        var extensions = new Dictionary<string, object?> { [ProblemCodes.MemberName] = code };

        return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, extensions: extensions);
    }

    private static string ThrowUnexpected()
    {
        throw new InvalidOperationException(UnexpectedMessage);
    }

    private static async Task ThrowAfterTheAnswerBeganAsync(HttpContext context)
    {
        await context.Response.StartAsync(context.RequestAborted);
        throw new InvalidOperationException(UnexpectedMessage);
    }
}
