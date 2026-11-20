using Bravo.Desktop.Policies;
using Xunit;

namespace Bravo.Desktop.Tests.Policies;

public sealed class PolicyReadFailureDialogTests
{
    // The administrator, whom the user asks, learns where to look.
    [Fact]
    public void CreateText_Key_SaysThatTheApplicationDoesNotStartAndNamesTheKey()
    {
        const string KeyName = @"HKEY_LOCAL_MACHINE\Software\Policies\SQLBI\Bravo\OptionSettings";

        var text = PolicyReadFailureDialog.CreateText(KeyName);

        Assert.StartsWith("Bravo for Power BI cannot start", text);
        Assert.Contains(KeyName, text);
    }
}
