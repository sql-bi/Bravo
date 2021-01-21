using System;
using System.Globalization;

namespace Sqlbi.Bravo.UI.Framework.ValueConverters
{
    internal class SimplifyNumberConverter : BaseValueConverter<SimplifyNumberConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "??";
            }

            var asLong = System.Convert.ToInt64(value);

            const long OneMillion = 1000000;
            const long OneThousand = 1000;

            // Not using Humanizr's `ToMetric()` as it doesn't round or handle millions
            if (asLong > OneMillion)
            {
                var mod = asLong % OneMillion;

                if (mod == 0)
                {
                    return $"{asLong / OneMillion}M";
                }
                else
                {
                    // Not rounding (for simplicity)
                    var firstDecimal = mod.ToString("000000")[0];

                    return firstDecimal == '0'
                        ? $"{asLong / OneMillion}M"
                        : $"{(asLong - mod) / OneMillion}.{firstDecimal}M";
                }
            }
            else if (asLong > OneThousand)
            {
                var mod = asLong % OneThousand;

                if (mod == 0)
                {
                    return $"{asLong / OneThousand}K";
                }
                else
                {
                    // Not rounding (for simplicity)
                    var firstDecimal = mod.ToString("000")[0];

                    return firstDecimal == '0'
                        ? $"{asLong / OneThousand}K"
                        : $"{(asLong - mod) / OneThousand}.{firstDecimal}K";
                }
            }
            else
            {
                return asLong.ToString();
            }
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
