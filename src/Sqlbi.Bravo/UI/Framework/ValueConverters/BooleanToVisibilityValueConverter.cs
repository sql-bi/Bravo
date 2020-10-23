using System;
using System.Globalization;
using System.Windows;

namespace Sqlbi.Bravo.UI.Framework.ValueConverters
{
    internal class BooleanToVisibilityValueConverter : BaseValueConverter<BooleanToVisibilityValueConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter == null
                ? (bool)value ? Visibility.Hidden : Visibility.Visible
                : (bool)value ? Visibility.Visible : Visibility.Hidden;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}