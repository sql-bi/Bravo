using System;
using System.Net;
using Bravo.Platform.Windows.Interop;
using Bravo.Platform.Windows.Network;
using Xunit;

namespace Bravo.Platform.Windows.Tests.Network;

public sealed class WindowsProxyCredentialStoreTests
{
    // A user and a password of the tests, which exist nowhere.
    private const string UserName = "test-user";
    private const string Password = "päss wörd ✓";

    [Fact]
    public void Write_ThenRead_GivesTheCredentialsBack()
    {
        HostedRunner.SkipElsewhere("in the Windows Credential Manager");
        var targetName = NewTargetName();
        var store = CreateStore(targetName);
        try
        {
            store.Write(new NetworkCredential(UserName, Password, "CONTOSO"));

            var read = store.Read();

            Assert.NotNull(read);
            Assert.Equal((UserName, "CONTOSO", Password), (read.UserName, read.Domain, read.Password));
        }
        finally
        {
            CredentialStoreInterop.DeleteGeneric(targetName);
        }
    }

    [Fact]
    public void Write_Twice_KeepsTheLastCredentials()
    {
        HostedRunner.SkipElsewhere("in the Windows Credential Manager");
        var targetName = NewTargetName();
        var store = CreateStore(targetName);
        try
        {
            store.Write(new NetworkCredential("first-user", "first-password"));
            store.Write(new NetworkCredential(UserName, Password));

            var read = store.Read();

            Assert.Equal((UserName, Password), (read?.UserName, read?.Password));
        }
        finally
        {
            CredentialStoreInterop.DeleteGeneric(targetName);
        }
    }

    // The shape that 1.0.x writes and reads: the user name as text, and the password as its UTF-16 characters,
    // with no terminator.
    [Fact]
    public void Write_AnyCredentials_KeepsThePasswordAsItsUtf16Characters()
    {
        HostedRunner.SkipElsewhere("in the Windows Credential Manager");
        var targetName = NewTargetName();
        try
        {
            CreateStore(targetName).Write(new NetworkCredential(UserName, Password));

            var kept = CredentialStoreInterop.ReadGeneric(targetName);

            Assert.Equal((UserName, Password), kept);
        }
        finally
        {
            CredentialStoreInterop.DeleteGeneric(targetName);
        }
    }

    // Written by a program of Windows, and not by the code that reads it.
    [Fact]
    public void Read_CredentialsThatAnotherProgramWrote_AreRead()
    {
        HostedRunner.SkipElsewhere("in the Windows Credential Manager");
        var targetName = NewTargetName();
        try
        {
            CredentialCommand.Write(targetName, UserName, "test-password");

            var read = CreateStore(targetName).Read();

            Assert.Equal((UserName, "test-password"), (read?.UserName, read?.Password));
        }
        finally
        {
            CredentialStoreInterop.DeleteGeneric(targetName);
        }
    }

    [Fact]
    public void Read_NoCredentials_IsNull()
    {
        HostedRunner.SkipElsewhere("in the Windows Credential Manager");
        var logger = new RecordingLogger<WindowsProxyCredentialStore>();
        var store = new WindowsProxyCredentialStore(NewTargetName(), logger);

        var read = store.Read();

        Assert.Null(read);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void Delete_KeptCredentialsAndThenNone_LeavesNone()
    {
        HostedRunner.SkipElsewhere("in the Windows Credential Manager");
        var targetName = NewTargetName();
        var store = CreateStore(targetName);
        try
        {
            store.Write(new NetworkCredential(UserName, Password));

            store.Delete();
            store.Delete();

            Assert.Null(store.Read());
        }
        finally
        {
            CredentialStoreInterop.DeleteGeneric(targetName);
        }
    }

    private static string NewTargetName()
    {
        return $"Bravo.Tests/{Guid.NewGuid():N}/proxy";
    }

    private static WindowsProxyCredentialStore CreateStore(string targetName)
    {
        return new WindowsProxyCredentialStore(targetName, new RecordingLogger<WindowsProxyCredentialStore>());
    }
}
