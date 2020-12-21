using Sqlbi.Bravo.UI.Controls.Parser;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sqlbi.Bravo.UI.Controls
{
    public partial class DaxFormattedTextBlock : UserControl
    {
        public DaxFormattedTextBlock() => InitializeComponent();

        public string UncoloredText
        {
            get => (string)GetValue(UncoloredTextProperty);
            set => SetValue(UncoloredTextProperty, value);
        }

        public static readonly DependencyProperty UncoloredTextProperty =
           DependencyProperty.Register("UncoloredText", typeof(string), typeof(DaxFormattedTextBlock), new PropertyMetadata("", UncoloredTextChanged));

        private static void UncoloredTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as DaxFormattedTextBlock;

            control.ActualDisplayedText.Inlines.Clear();

            foreach (var line in e.NewValue.ToString().Split("\r\n"))
            {
                var elements = SimpleDaxFormatter.FormatLine(line);

                foreach (var element in elements)
                {
                    control.ActualDisplayedText.Inlines.Add(element);
                }

                control.ActualDisplayedText.Inlines.Add(new LineBreak());
            }
        }
    }
}
