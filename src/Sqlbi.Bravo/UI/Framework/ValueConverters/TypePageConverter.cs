using System;
using System.Globalization;
using System.Linq;

namespace Sqlbi.Bravo.UI.Framework.ValueConverters
{
    internal class TypePageConverter : BaseValueConverter<TypePageConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return $"/UI/Views/{value.ToString().Split('.').Last()}.xaml";
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
