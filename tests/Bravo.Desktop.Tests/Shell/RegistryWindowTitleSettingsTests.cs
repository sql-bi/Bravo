using Bravo.Desktop.Shell;
using Xunit;

namespace Bravo.Desktop.Tests.Shell;

public sealed class RegistryWindowTitleSettingsTests
{
    [Fact]
    public void IsVersionHiddenValue_One_IsHidden()
    {
        Assert.True(RegistryWindowTitleSettings.IsVersionHiddenValue(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1)]
    public void IsVersionHiddenValue_AnyOtherNumber_IsShown(int value)
    {
        Assert.False(RegistryWindowTitleSettings.IsVersionHiddenValue(value));
    }

    // 1.0.x reads a DWORD alone: the text "1" and a QWORD do not hide the version.
    [Fact]
    public void IsVersionHiddenValue_NoValueOrAnotherKind_IsShown()
    {
        Assert.False(RegistryWindowTitleSettings.IsVersionHiddenValue(null));
        Assert.False(RegistryWindowTitleSettings.IsVersionHiddenValue("1"));
        Assert.False(RegistryWindowTitleSettings.IsVersionHiddenValue(1L));
    }
}
