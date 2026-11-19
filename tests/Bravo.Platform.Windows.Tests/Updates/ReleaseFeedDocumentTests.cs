using System;
using Bravo.Core.Errors;
using Bravo.Platform.Windows.Updates;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Updates;

public sealed class ReleaseFeedDocumentTests
{
    [Fact]
    public void Parse_FeedWithMembersThatItDoesNotKnow_ReadsTheRelease()
    {
        var text = """
            {
              "channel": "Stable",
              "version": "1.1.0.188",
              "download": "https://github.com/sql-bi/Bravo/releases/download/v1.1.0/Bravo.1.1.0.x64.msi",
              "changelog": "https://github.com/sql-bi/Bravo/releases/tag/v1.1.0",
              "notes": ["a later member"]
            }
            """;

        var release = ReleaseFeedDocument.Parse(text);

        Assert.Equal(new Version(1, 1, 0, 188), release.Version);
        Assert.Equal(new Uri("https://github.com/sql-bi/Bravo/releases/download/v1.1.0/Bravo.1.1.0.x64.msi"), release.Download);
        Assert.Equal(new Uri("https://github.com/sql-bi/Bravo/releases/tag/v1.1.0"), release.ReleaseNotes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html>Sign in to the proxy</html>")]
    [InlineData("[]")]
    [InlineData("""{ "download": "https://github.com/a.msi", "changelog": "https://github.com/" }""")]
    [InlineData("""{ "version": "1.1", "download": "https://github.com/a.msi" }""")]
    [InlineData("""{ "version": "soon", "download": "https://github.com/a.msi", "changelog": "https://github.com/" }""")]
    [InlineData("""{ "version": 1.1, "download": "https://github.com/a.msi", "changelog": "https://github.com/" }""")]
    [InlineData("""{ "version": "1.1.0.188", "download": "/a.msi", "changelog": "https://github.com/" }""")]
    [InlineData("""{ "version": "1.1.0.188", "download": "file:///C:/a.msi", "changelog": "https://github.com/" }""")]
    [InlineData("""{ "version": "1.1.0.188", "download": "https://attacker.test/a.msi", "changelog": "https://github.com/" }""")]
    [InlineData("""{ "version": "1.1.0.188", "download": "https://github.com/a.msi", "changelog": "http://127.0.0.1/" }""")]
    [InlineData("""{ "version": "1.1.0.188", "download": "https://github.com/a.msi", "changelog": null }""")]
    public void Parse_TextThatNamesNoRelease_FailsWithTheCodeOfTheCheck(string text)
    {
        var exception = Assert.Throws<BravoException>(() => ReleaseFeedDocument.Parse(text));

        Assert.Equal(ErrorCode.UpdateCheckFailed, exception.Code);
    }
}
