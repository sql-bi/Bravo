using System;
using System.Globalization;
using System.Windows.Data;

namespace Sqlbi.Bravo.UI.Framework.ValueConverters
{
    internal class ConnectionIconConverter
         : BaseValueConverter<ConnectionIconConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value.ToString()) switch
            {
                "ConnectedPowerBiDataset" => new Controls.DatasetIcon(),
                "ActivePowerBiWindow" => new Controls.DesktopIcon(),
                "VertipaqAnalyzerFile" => new Controls.VertipaqFileIcon(),
                _ => null,
            };
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
