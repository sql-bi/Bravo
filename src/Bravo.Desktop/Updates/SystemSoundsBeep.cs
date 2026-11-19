using System.Media;
using Bravo.Core.Updates;

namespace Bravo.Desktop.Updates;

internal sealed class SystemSoundsBeep : ISystemBeep
{
    public void Play()
    {
        SystemSounds.Beep.Play();
    }
}
