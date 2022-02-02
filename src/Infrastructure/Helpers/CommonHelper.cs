namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using System.Windows.Forms;

    internal static class CommonHelper
    {
        public static User32.KeyState GetKeyState(Keys key)
        {
            var state = User32.KeyState.None;
            {
                var retval = User32.GetKeyState((int)key);

                if ((retval & 0x8000) == 0x8000)
                    state |= User32.KeyState.Down;

                if ((retval & 1) == 1)
                    state |= User32.KeyState.Toggled;
            }
            return state;
        }

        public static bool IsKeyDown(Keys key)
        {
            var state = GetKeyState(key);

            return state.HasFlag(User32.KeyState.Down);
        }
    }
}
