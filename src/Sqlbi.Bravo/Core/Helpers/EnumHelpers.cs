using Sqlbi.Bravo.Core.Services;

namespace Sqlbi.Bravo.Core.Helpers
{
    internal static class EnumHelpers
    {
        public static DaxFormatterServiceTabularObjectType WithFlag(this DaxFormatterServiceTabularObjectType item, DaxFormatterServiceTabularObjectType flag, bool set) => set ? item.SetFlag(flag) : item.RemoveFlag(flag);

        public static DaxFormatterServiceTabularObjectType SetFlag(this DaxFormatterServiceTabularObjectType item, DaxFormatterServiceTabularObjectType flag) => item | flag;

        public static DaxFormatterServiceTabularObjectType RemoveFlag(this DaxFormatterServiceTabularObjectType item, DaxFormatterServiceTabularObjectType flag) => item & ~flag;
    }
}