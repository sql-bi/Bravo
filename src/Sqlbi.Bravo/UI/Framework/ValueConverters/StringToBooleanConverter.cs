using System;
using System.Globalization;
using System.Windows.Data;

namespace Sqlbi.Bravo.UI.Framework.ValueConverters
{
    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string str)
            {
                System.Diagnostics.Debug.WriteLine($"Convert '{str}'");
                return value.ToString().Equals(str, StringComparison.InvariantCultureIgnoreCase);
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => parameter is string str ? str : null;
    }
}
