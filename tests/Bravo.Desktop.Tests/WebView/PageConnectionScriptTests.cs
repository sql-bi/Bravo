using System;
using Bravo.Desktop.WebView;
using Xunit;

namespace Bravo.Desktop.Tests.WebView;

/// <summary>
/// The text of the script. What the script does in a document is tested with the real engine.
/// </summary>
public sealed class PageConnectionScriptTests
{
    private static readonly Uri s_apiAddress = new("http://127.0.0.1:50123/");

    [Fact]
    public void Create_AddressOfTheApi_IsWrittenWithoutASlashAtItsEnd()
    {
        var script = PageConnectionScript.Create(s_apiAddress, "token");

        Assert.Contains("address: \"http://127.0.0.1:50123\",", script);
    }

    [Fact]
    public void Create_Script_DefinesTheObjectOnlyInADocumentOfTheOriginOfThePage()
    {
        var script = PageConnectionScript.Create(s_apiAddress, "token");

        Assert.Contains("if (window.location.origin !== \"https://bravo.invalid\")", script);
        Assert.Contains("Object.defineProperty(window, \"bravoHost\"", script);
    }

    // A token is letters, digits, hyphens and underscores. The script must stay one script whatever it is given.
    [Fact]
    public void Create_TextThatWouldEndTheLiteral_IsEscaped()
    {
        var script = PageConnectionScript.Create(s_apiAddress, "\"}); alert('x'); ({\"</script>");

        Assert.DoesNotContain("alert('x')", script);
        Assert.DoesNotContain("</script>", script);
    }
}
