using Sqlbi.Bravo.Core.Client.Http;

namespace Sqlbi.Bravo.Core.Helpers
{
    internal static class EnumHelpers
    {
        public static DaxFormatterTabularObjectType WithFlag(this DaxFormatterTabularObjectType item, DaxFormatterTabularObjectType flag, bool set) => set ? item.SetFlag(flag) : item.RemoveFlag(flag);

        public static DaxFormatterTabularObjectType SetFlag(this DaxFormatterTabularObjectType item, DaxFormatterTabularObjectType flag) => item | flag;

        public static DaxFormatterTabularObjectType RemoveFlag(this DaxFormatterTabularObjectType item, DaxFormatterTabularObjectType flag) => item & ~flag;
    }
}