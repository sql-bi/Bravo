using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.Views;
using System;
using System.Globalization;

namespace Sqlbi.Bravo.UI.Framework.ValueConverters
{
    internal class ApplicationViewValueConverter : BaseValueConverter<ApplicationViewValueConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var page = (ApplicationView)value;

            return page switch
            {
                ApplicationView.Settings => new SettingsView(),
                ApplicationView.DaxFormatter => new DaxFormatterView(),
                _ => null,
            };
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
