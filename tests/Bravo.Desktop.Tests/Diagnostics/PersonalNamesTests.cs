using Bravo.Desktop.Diagnostics;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class PersonalNamesTests
{
    private readonly PersonalNames _names = new(@"C:\Users\jdoe\", "jdoe", "LAPTOP-42");

    [Fact]
    public void RemoveFrom_PathInTheProfile_KeepsThePathWithoutTheProfile()
    {
        var text = _names.RemoveFrom(@"Could not find file 'c:\users\JDOE\AppData\Local\Bravo\usersettings.json'.");

        Assert.Equal(@"Could not find file '%USERPROFILE%\AppData\Local\Bravo\usersettings.json'.", text);
    }

    [Fact]
    public void RemoveFrom_NamesOfTheUserAndOfTheComputer_ReplacesThem()
    {
        var text = _names.RemoveFrom(@"Login failed for user 'CONTOSO\jdoe' on laptop-42.");

        Assert.Equal(@"Login failed for user 'CONTOSO\%USERNAME%' on %COMPUTERNAME%.", text);
    }

    // A name can also be a part of another word, which stays as it is.
    [Fact]
    public void RemoveFrom_NameInsideAnotherWord_KeepsTheWord()
    {
        Assert.Equal("The jdoe2 table.", _names.RemoveFrom("The jdoe2 table."));
    }

    // The folder of another user, whose name begins with the one of this user.
    [Fact]
    public void RemoveFrom_FolderThatBeginsWithTheProfile_KeepsTheFolder()
    {
        var names = new PersonalNames(@"C:\Users\jo", "jo", "LAPTOP-42");
        const string Text = @"Could not open 'C:\Users\john\data.pbix'.";

        Assert.Equal(Text, names.RemoveFrom(Text));
    }

    // Windows cuts its name of the computer at 15 characters; the network has the whole one.
    [Fact]
    public void RemoveFrom_NameOfTheComputerOnTheNetwork_ReplacesTheWholeName()
    {
        var names = new PersonalNames(@"C:\Users\jdoe", "jdoe", "FINANCE-LAPTOP-", "finance-laptop-0042");

        var text = names.RemoveFrom(@"The path '\\finance-laptop-0042\share' was not found.");

        Assert.Equal(@"The path '\\%COMPUTERNAME%\share' was not found.", text);
    }
}
