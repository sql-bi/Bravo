using System.Net;
using Bravo.Core.Network;
using Xunit;

namespace Bravo.Core.Tests.Network;

public sealed class CredentialUserNameTests
{
    private const string Password = "päss wörd ✓";

    [Fact]
    public void ToCredential_UserOfADomain_HasTheDomainApart()
    {
        var credential = CredentialUserName.ToCredential(@"CONTOSO\test-user", Password);

        Assert.Equal("test-user", credential.UserName);
        Assert.Equal("CONTOSO", credential.Domain);
        Assert.Equal(Password, credential.Password);
    }

    [Theory]
    [InlineData("test-user")]
    [InlineData("test-user@contoso.test")]
    [InlineData(@"a\b\c")]
    public void ToCredential_AnyOtherName_IsTheUserNameAsItIs(string userName)
    {
        var credential = CredentialUserName.ToCredential(userName, Password);

        Assert.Equal(userName, credential.UserName);
        Assert.Equal(string.Empty, credential.Domain);
    }

    [Theory]
    [InlineData("test-user", "", "test-user")]
    [InlineData("test-user", "CONTOSO", @"CONTOSO\test-user")]
    [InlineData("test-user@contoso.test", "", "test-user@contoso.test")]
    public void FromCredential_AnyCredential_IsReadBackAsTheSameUser(string userName, string domain, string expected)
    {
        var credential = new NetworkCredential(userName, Password, domain);

        var text = CredentialUserName.FromCredential(credential);

        Assert.Equal(expected, text);
        var readBack = CredentialUserName.ToCredential(text, Password);
        Assert.Equal((userName, domain), (readBack.UserName, readBack.Domain));
    }
}
