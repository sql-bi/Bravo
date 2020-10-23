using System;
using System.Globalization;

namespace Sqlbi.Bravo.UI.Framework.ValueConverters
{
    internal class InverseBooleanValueConverter : BaseValueConverter<InverseBooleanValueConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) => !(bool)value;

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => !(bool)value;
    }
}