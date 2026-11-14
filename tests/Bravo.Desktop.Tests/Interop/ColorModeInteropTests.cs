using System.Collections.Generic;
using System.Windows.Forms;
using Bravo.Desktop.Interop;
using Windows.Win32;
using Xunit;

namespace Bravo.Desktop.Tests.Interop;

/// <summary>
/// The requests to Windows for the frame of a window, on a real window of the test that is never shown.
/// What Windows draws is not seen here: the title bar is checked by hand.
/// </summary>
public sealed class ColorModeInteropTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryUseDarkFrame_OnTheWindowsThatRunsTheTests_IsAccepted(bool isDark)
    {
        using var window = new RecordingWindow();

        var accepted = ColorModeInterop.TryUseDarkFrame(window.Handle, isDark);

        Assert.True(accepted);
    }

    // The frame is left drawn as the window is: the last thing that the window is told is its real state.
    [Theory]
    [InlineData(true, new[] { false, true })]
    [InlineData(false, new[] { true, false })]
    public void RedrawFrame_Window_IsToldTheOppositeOfItsStateAndThenItsState(bool isActive, bool[] told)
    {
        using var window = new RecordingWindow();
        _ = window.Handle;
        window.Activations.Clear();

        ColorModeInterop.RedrawFrame(window.Handle, isActive);

        Assert.Equal(told, window.Activations);
    }

    [Fact]
    public void IsSettingChange_OnlyTheMessageOfAChangedSetting_IsOne()
    {
        Assert.True(ColorModeInterop.IsSettingChange((int)PInvoke.WM_SETTINGCHANGE));
        Assert.False(ColorModeInterop.IsSettingChange((int)PInvoke.WM_NCACTIVATE));
    }

    // Records what the window is told about its activation. A message sent to a window of the calling
    // thread is delivered at once, so the test needs no message loop.
    private sealed class RecordingWindow : Form
    {
        public List<bool> Activations { get; } = [];

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)PInvoke.WM_NCACTIVATE)
            {
                Activations.Add(m.WParam != 0);
            }

            base.WndProc(ref m);
        }
    }
}
