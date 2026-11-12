using Bravo.Api.Security;
using Xunit;

namespace Bravo.Api.Tests.Security;

public sealed class ApiTokenTests
{
    [Fact]
    public void Constructor_TwoTokens_AreDifferent()
    {
        var first = new ApiToken();
        var second = new ApiToken();

        Assert.NotEqual(first.Value, second.Value);
    }

    [Fact]
    public void Value_NewToken_Is32RandomBytesWrittenForAHeader()
    {
        var token = new ApiToken();

        // 32 bytes as base64url, without padding.
        Assert.Matches("^[A-Za-z0-9_-]{43}$", token.Value);
    }

    [Fact]
    public void Matches_ItsOwnValue_ReturnsTrue()
    {
        var token = new ApiToken();

        Assert.True(token.Matches(token.Value));
    }

    [Fact]
    public void Matches_ValueOfAnotherToken_ReturnsFalse()
    {
        var token = new ApiToken();

        Assert.False(token.Matches(new ApiToken().Value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void Matches_ValueOfAnotherLength_ReturnsFalse(string candidate)
    {
        var token = new ApiToken();

        Assert.False(token.Matches(candidate));
    }

    [Fact]
    public void Matches_ValueWithCharactersThatAreNotAscii_ReturnsFalse()
    {
        var token = new ApiToken();
        var candidate = new string('é', token.Value.Length);

        Assert.False(token.Matches(candidate));
    }

    [Fact]
    public void ToString_Token_DoesNotShowTheValue()
    {
        var token = new ApiToken();

        Assert.DoesNotContain(token.Value, token.ToString());
    }
}
