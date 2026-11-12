using System;
using System.Diagnostics;
using System.Security.Principal;
using Bravo.Platform.Windows.SingleInstance;
using Xunit;

namespace Bravo.Platform.Windows.Tests.SingleInstance;

public sealed class InstanceNamesTests
{
    [Fact]
    public void ForCurrentUserSession_MutexName_HasTheApplicationAndTheUser()
    {
        using var identity = WindowsIdentity.GetCurrent();

        var names = InstanceNames.ForCurrentUserSession("Application");

        Assert.Equal($"Application.{identity.User}", names.MutexName);
    }

    [Fact]
    public void ForCurrentUserSession_PipeName_HasTheApplicationTheSessionAndTheUser()
    {
        using var identity = WindowsIdentity.GetCurrent();
        using var process = Process.GetCurrentProcess();

        var names = InstanceNames.ForCurrentUserSession("Application");

        Assert.Equal($"Application.{process.SessionId}.{identity.User}", names.PipeName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ForCurrentUserSession_NoApplication_Throws(string applicationId)
    {
        Assert.Throws<ArgumentException>(() => InstanceNames.ForCurrentUserSession(applicationId));
    }
}
