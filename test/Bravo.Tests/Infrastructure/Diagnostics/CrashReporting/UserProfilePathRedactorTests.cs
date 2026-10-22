using System.Collections.Generic;
using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics.CrashReporting;

public class UserProfilePathRedactorTests
{
    private static readonly UserProfilePathRedactor s_redactor = new(
    [
        new(@"C:\Users\jane.doe", "%USERPROFILE%"),
        new(@"C:\Users\jane.doe\AppData\Local", "%LOCALAPPDATA%"),
        new(@"C:\Users\jane.doe\AppData\Roaming\", "%APPDATA%"),
    ]);

    [Theory]
    [InlineData(@"C:\Users\jane.doe\AppData\Local\Bravo", @"%LOCALAPPDATA%\Bravo")]
    [InlineData(@"C:\Users\jane.doe\AppData\Roaming\Bravo", @"%APPDATA%\Bravo")]
    [InlineData(@"C:\Users\jane.doe\Documents\Sales.pbix", @"%USERPROFILE%\Documents\Sales.pbix")]
    [InlineData(@"C:\Users\jane.doe", "%USERPROFILE%")]
    public void Redact_ReplacesTheMostSpecificFolder(string text, string expected)
    {
        Assert.Equal(expected, s_redactor.Redact(text));
    }

    [Fact]
    public void Redact_IgnoresTheCaseOfThePath()
    {
        Assert.Equal(@"%LOCALAPPDATA%\Bravo", s_redactor.Redact(@"c:\users\JANE.DOE\appdata\local\Bravo"));
    }

    [Fact]
    public void Redact_ReplacesEveryOccurrenceInTheText()
    {
        var text = @"File 'C:\Users\jane.doe\Desktop\a.pbix' was copied to C:\Users\jane.doe\AppData\Local\Temp.";

        Assert.Equal(@"File '%USERPROFILE%\Desktop\a.pbix' was copied to %LOCALAPPDATA%\Temp.", s_redactor.Redact(text));
    }

    [Theory]
    [InlineData(@"Access denied: C:\Users\jane.doe.", "Access denied: %USERPROFILE%.")]
    [InlineData(@"Not found: C:\Users\jane.doe: retry", "Not found: %USERPROFILE%: retry")]
    [InlineData(@"(C:\Users\jane.doe)", "(%USERPROFILE%)")]
    public void Redact_ReplacesAFolderBeforePunctuation(string text, string expected)
    {
        Assert.Equal(expected, s_redactor.Redact(text));
    }

    [Theory]
    [InlineData("C:/Users/jane.doe/AppData/Local/Bravo", "%LOCALAPPDATA%/Bravo")]
    [InlineData("file:///C:/Users/jane.doe/Documents/Sales.pbix", "file:///%USERPROFILE%/Documents/Sales.pbix")]
    public void Redact_ReplacesAFolderWrittenWithForwardSlashes(string text, string expected)
    {
        Assert.Equal(expected, s_redactor.Redact(text));
    }

    [Theory]
    [InlineData(@"C:\Users\jane.doe2\Documents")]
    [InlineData(@"C:\Users\jane.doe.old")]
    [InlineData(@"C:\Users\jane.doe.old\Documents")]
    [InlineData("no paths in this message")]
    public void Redact_LeavesOtherTextUnchanged(string text)
    {
        Assert.Equal(text, s_redactor.Redact(text));
    }

    [Fact]
    public void Redact_IgnoresFoldersThatAreEmpty()
    {
        var redactor = new UserProfilePathRedactor([new KeyValuePair<string, string>("", "%EMPTY%")]);

        Assert.Equal(@"C:\Data", redactor.Redact(@"C:\Data"));
    }

    [Fact]
    public void Redact_WritesTheVariableLiterally()
    {
        var redactor = new UserProfilePathRedactor([new KeyValuePair<string, string>(@"C:\Users\jane", "$1")]);

        Assert.Equal(@"$1\Documents", redactor.Redact(@"C:\Users\jane\Documents"));
    }
}
