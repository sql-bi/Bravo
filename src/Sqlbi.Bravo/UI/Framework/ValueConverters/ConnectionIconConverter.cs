using Sqlbi.Bravo.UI.Controls;
using Sqlbi.Bravo.UI.DataModel;
using System;
using System.Globalization;
using System.Windows.Controls;

namespace Sqlbi.Bravo.UI.Framework.ValueConverters
{
    internal class ConnectionIconConverter: BaseValueConverter<ConnectionIconConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var connectionType = Enum.Parse(typeof(BiConnectionType), System.Convert.ToString(value));
            
            UserControl control = connectionType switch
            {
                BiConnectionType.ConnectedPowerBiDataset => new DatasetIcon(),
                BiConnectionType.ActivePowerBiWindow => new DesktopIcon(),
                BiConnectionType.VertipaqAnalyzerFile => new VertipaqFileIcon(),
                _ => null,
            };

            return control;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}