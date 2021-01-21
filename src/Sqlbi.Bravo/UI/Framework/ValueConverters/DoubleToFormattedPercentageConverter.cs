using System;
using System.Globalization;

namespace Sqlbi.Bravo.UI.Framework.ValueConverters
{
    internal class DoubleToFormattedPercentageConverter : BaseValueConverter<DoubleToFormattedPercentageConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val)
            {
                var str = (val * 100).ToString();

                // This is a crude formatter for showing one significant figure
                // For simplicity it rounds down.
                if (str.StartsWith("0"))
                {
                    for (var i = 2; i < str.Length; i++)
                    {
                        if (str[i] != '0')
                        {
                            return $"{str.Substring(0, i + 1)} %";
                        }
                        else
                        {
                            if (i > 4)
                            {
                                break;
                            }
                        }
                    }

                    return "0.0000 %";
                }
                else
                {
                    return (val * 100).ToString("#0.#' %'");
                }
            }

            return string.Empty;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
