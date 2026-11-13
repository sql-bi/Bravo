using System;
using Bravo.Core.Settings;
using Bravo.Desktop.Shell;
using Xunit;

namespace Bravo.Desktop.Tests.Shell;

public sealed class WindowColorsTests
{
    [Theory]
    [InlineData(Theme.Light, false)]
    [InlineData(Theme.Light, true)]
    public void For_LightTheme_IsLightWhateverTheSystem(Theme theme, bool isSystemDark)
    {
        Assert.Same(WindowColors.Light, WindowColors.For(theme, isSystemDark));
    }

    [Theory]
    [InlineData(Theme.Dark, false)]
    [InlineData(Theme.Dark, true)]
    public void For_DarkTheme_IsDarkWhateverTheSystem(Theme theme, bool isSystemDark)
    {
        Assert.Same(WindowColors.Dark, WindowColors.For(theme, isSystemDark));
    }

    [Fact]
    public void For_AutoTheme_FollowsTheSystem()
    {
        Assert.Same(WindowColors.Dark, WindowColors.For(Theme.Auto, isSystemDark: true));
        Assert.Same(WindowColors.Light, WindowColors.For(Theme.Auto, isSystemDark: false));
    }

    [Fact]
    public void For_ThemeThatDoesNotExist_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WindowColors.For((Theme)99, isSystemDark: false));
    }

    [Fact]
    public void Dark_Frame_IsDarkAndLightIsNot()
    {
        Assert.True(WindowColors.Dark.IsDark);
        Assert.False(WindowColors.Light.IsDark);
    }
}
