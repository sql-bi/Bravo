using System;
using System.Windows.Data;

namespace Sqlbi.Bravo.UI.Framework.ValueConverters
{
    public class BooleanOrConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            foreach (var booleanValue in values)
            {
                if ((booleanValue is bool) == false)
                {
                    throw new ApplicationException("BooleanOrConverter only accepts boolean as datatype");
                }

                if ((bool)booleanValue == true)
                {
                    return true;
                }
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
}
