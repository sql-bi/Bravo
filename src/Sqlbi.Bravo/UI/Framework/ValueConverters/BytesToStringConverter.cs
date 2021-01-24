using Humanizer;
using System;
using System.Globalization;

namespace Sqlbi.Bravo.UI.Framework.ValueConverters
{
    internal class BytesToStringConverter : BaseValueConverter<BytesToStringConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is long val
                ? val > 0
                    ? val.Bytes().ToString("#.#")
                    : string.Empty
                : string.Empty;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
