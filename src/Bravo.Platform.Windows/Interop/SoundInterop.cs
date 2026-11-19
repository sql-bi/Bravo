using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Bravo.Platform.Windows.Interop;

internal static class SoundInterop
{
    /// <summary>
    /// Plays the sound that Windows plays for a message with no icon, the one of <c>SystemSounds.Beep</c>.
    /// Where the sound cannot be played, nothing happens.
    /// </summary>
    public static void PlayDefaultBeep()
    {
        _ = PInvoke.MessageBeep(MESSAGEBOX_STYLE.MB_OK);
    }
}
