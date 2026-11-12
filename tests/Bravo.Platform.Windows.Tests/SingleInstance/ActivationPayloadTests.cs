using System.Text;
using Bravo.Core.Activation;
using Bravo.Platform.Windows.SingleInstance;
using Xunit;

namespace Bravo.Platform.Windows.Tests.SingleInstance;

public sealed class ActivationPayloadTests
{
    // The text that another version of the application reads: a change here needs a new protocol version.
    [Fact]
    public void Encode_Request_WritesItsMembersAsJson()
    {
        var request = new ActivationRequest
        {
            ServerName = "localhost:12345",
            DatabaseName = "db",
            ParentProcessId = 42,
            CommandLineErrors = ["wrong"],
        };

        var payload = ActivationPayload.Encode(request);

        const string Expected =
            """{"serverName":"localhost:12345","databaseName":"db","parentProcessId":42,"commandLineErrors":["wrong"]}""";
        Assert.Equal(Expected, Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public void Encode_EmptyRequest_WritesOnlyTheListOfErrors()
    {
        var payload = ActivationPayload.Encode(ActivationRequest.Empty);

        Assert.Equal("""{"commandLineErrors":[]}""", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public void TryDecode_EncodedRequest_ReturnsTheSameValues()
    {
        var request = new ActivationRequest
        {
            ServerName = "localhost:12345",
            DatabaseName = "db",
            ParentProcessId = 42,
            CommandLineErrors = ["wrong", "worse"],
        };

        var decoded = ActivationPayload.TryDecode(ActivationPayload.Encode(request), out var result);

        Assert.True(decoded);
        Assert.Equivalent(request, result, strict: true);
    }

    [Fact]
    public void TryDecode_ObjectWithNoMembers_ReturnsARequestWithNoValues()
    {
        var decoded = ActivationPayload.TryDecode("{}"u8, out var result);

        Assert.True(decoded);
        Assert.Equivalent(ActivationRequest.Empty, result, strict: true);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("""{"serverName":42}""")]
    [InlineData("""{"commandLineErrors":null}""")]
    public void TryDecode_PayloadThatIsNotARequest_ReturnsFalse(string payload)
    {
        var decoded = ActivationPayload.TryDecode(Encoding.UTF8.GetBytes(payload), out var result);

        Assert.False(decoded);
        Assert.Null(result);
    }
}
