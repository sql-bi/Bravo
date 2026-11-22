using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Bravo.Api.Diagnostics;
using Bravo.Api.Problems;
using Xunit;

namespace Bravo.Api.Tests.Diagnostics;

public sealed class LogEndpointsTests
{
    private static readonly DateTimeOffset s_time = new(2026, 11, 22, 9, 30, 15, 123, TimeSpan.Zero);

    [Fact]
    public async Task GetLog_RecordsKept_ReturnsThemWithWhatTheyTellAndNoMemberWithoutAValue()
    {
        await using var api = await TestApi.StartAsync();
        api.LogBuffer.Append(number => new LogRecord
        {
            Number = number,
            Time = s_time,
            Level = LogRecordLevel.Warning,
            Category = "Bravo.Core.Updates.UpdateCheck",
            EventId = 3,
            EventName = "FeedUnreachable",
            Message = "The feed cannot be read.",
            TraceId = "4bf92f3577b34da6a3ce929d0e0e4736",
        });
        api.LogBuffer.Append(number => new LogRecord
        {
            Number = number,
            Time = s_time,
            Level = LogRecordLevel.Error,
            Category = "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
            EventId = 1,
            Message = "An unhandled exception has occurred while executing the request.",
            Exception = "System.InvalidOperationException: The failure.",
        });

        using var response = await api.Client.GetAsync("/log", TestContext.Current.CancellationToken);

        using var expected = JsonDocument.Parse("""
            {
              "records": [
                {
                  "number": 1,
                  "time": "2026-11-22T09:30:15.123+00:00",
                  "level": "Warning",
                  "category": "Bravo.Core.Updates.UpdateCheck",
                  "eventId": 3,
                  "eventName": "FeedUnreachable",
                  "message": "The feed cannot be read.",
                  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
                },
                {
                  "number": 2,
                  "time": "2026-11-22T09:30:15.123+00:00",
                  "level": "Error",
                  "category": "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
                  "eventId": 1,
                  "message": "An unhandled exception has occurred while executing the request.",
                  "exception": "System.InvalidOperationException: The failure."
                }
              ],
              "next": 2
            }
            """);
        var actual = await response.ReadJsonAsync();
        Assert.True(JsonElement.DeepEquals(expected.RootElement, actual), actual.GetRawText());
    }

    [Fact]
    public async Task GetLog_AfterANumber_ReturnsTheRecordsThatFollowIt()
    {
        await using var api = await TestApi.StartAsync();
        Append(api, 3);

        using var response = await api.Client.GetAsync("/log?after=2", TestContext.Current.CancellationToken);

        var batch = await response.ReadJsonAsync();
        Assert.Equal(1, batch.GetProperty("records").GetArrayLength());
        Assert.Equal(3, batch.GetProperty("next").GetInt32());
    }

    [Fact]
    public async Task GetLog_NumberThatIsNotAllowed_IsAnswered400WithTheCodeOfTheValidation()
    {
        await using var api = await TestApi.StartAsync();

        using var response = await api.Client.GetAsync("/log?after=-1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(nameof(ApiProblemCode.ValidationFailed), await response.ReadProblemCodeAsync());
    }

    private static void Append(TestApi api, int count)
    {
        for (var i = 0; i < count; i++)
        {
            api.LogBuffer.Append(number => new LogRecord
            {
                Number = number,
                Time = s_time,
                Level = LogRecordLevel.Information,
                Category = "Bravo.Tests",
                EventId = 1,
                Message = "Something happened.",
            });
        }
    }
}
