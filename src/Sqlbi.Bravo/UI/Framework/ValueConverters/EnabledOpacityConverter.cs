using System;
using System.Globalization;

namespace Sqlbi.Bravo.UI.Framework.ValueConverters
{
    internal class EnabledOpacityConverter : BaseValueConverter<EnabledOpacityConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? 1.0 : 0.5;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
