namespace Sqlbi.Bravo.Infrastructure.Windows.Interop
{
    using System.Drawing;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct COLORREF
    {
        [FieldOffset(0)]
        private readonly uint Value;

        [FieldOffset(0)]
        public byte R;

        [FieldOffset(1)]
        public byte G;

        [FieldOffset(2)]
        public byte B;

        //private const uint CLR_NONE = uint.MaxValue;

        //private const uint CLR_DEFAULT = 4278190080u;

        //public static COLORREF None = new(uint.MaxValue);

        //public static COLORREF Default = new(4278190080u);

        public COLORREF(byte r, byte g, byte b)
        {
            Value = 0u;
            R = r;
            G = g;
            B = b;
        }

        public COLORREF(uint value)
        {
            R = 0;
            G = 0;
            B = 0;
            Value = (value & 0xFFFFFF);
        }

        public COLORREF(Color color) 
            : this(color.R, color.G, color.B)
        {
            if (color == Color.Transparent)
            {
                Value = uint.MaxValue;
            }
        }

        public static implicit operator Color(COLORREF colorRef)
        {
            if (colorRef.Value != uint.MaxValue)
            {
                return Color.FromArgb(colorRef.R, colorRef.G, colorRef.B);
            }

            return Color.Transparent;
        }

        public static implicit operator COLORREF(Color color)
        {
            return new COLORREF(color);
        }
    }
}
