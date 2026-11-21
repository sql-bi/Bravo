using Bravo.Desktop.Interop;
using Xunit;

namespace Bravo.Desktop.Tests.Interop;

/// <summary>
/// The blocks of the dialog box are packed and read in memory: nothing is shown, and nothing is kept.
/// </summary>
public sealed class CredentialDialogInteropTests
{
    [Theory]
    [InlineData("test-user", "test-password")]
    [InlineData(@"CONTOSO\test-user", "päss wörd ✓")]
    [InlineData("test-user@contoso.test", "")]
    public void Unpack_BlockThatPackWrote_GivesTheUserNameAndThePasswordBack(string userName, string password)
    {
        var block = CredentialDialogInterop.Pack(userName, password);

        var unpacked = CredentialDialogInterop.Unpack(block);

        Assert.Equal((userName, password), unpacked);
    }
}
