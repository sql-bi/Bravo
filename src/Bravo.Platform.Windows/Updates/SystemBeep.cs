using Bravo.Core.Updates;
using Bravo.Platform.Windows.Interop;

namespace Bravo.Platform.Windows.Updates;

public sealed class SystemBeep : ISystemBeep
{
    public void Play()
    {
        SoundInterop.PlayDefaultBeep();
    }
}
