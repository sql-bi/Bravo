using System;
using System.Windows.Forms;
using Bravo.Desktop.Shell;
using Xunit;

namespace Bravo.Desktop.Tests.Shell;

public sealed class RegistrySystemAppModeTests
{
    [Fact]
    public void IsDarkValue_Zero_IsDark()
    {
        Assert.True(RegistrySystemAppMode.IsDarkValue(0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(2)]
    public void IsDarkValue_AnyOtherNumber_IsLight(int value)
    {
        Assert.False(RegistrySystemAppMode.IsDarkValue(value));
    }

    // A version of Windows without the app mode has no value, and a value can be of another kind.
    [Fact]
    public void IsDarkValue_NoValueOrNoNumber_IsLight()
    {
        Assert.False(RegistrySystemAppMode.IsDarkValue(null));
        Assert.False(RegistrySystemAppMode.IsDarkValue("0"));
        Assert.False(RegistrySystemAppMode.IsDarkValue(0L));
    }

    // Windows Forms answers the same question from the same value, on Windows 11 only: where it answers,
    // the two agree.
    [Fact]
    public void IsDark_OnWindows11_IsWhatWindowsFormsSays()
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000), "Windows Forms reads the app mode on Windows 11 only.");
        Assert.SkipWhen(SystemInformation.HighContrast, "Windows Forms says light with a contrast theme.");

        var isDark = new RegistrySystemAppMode().IsDark;

        Assert.Equal(Application.SystemColorMode == SystemColorMode.Dark, isDark);
    }
}
