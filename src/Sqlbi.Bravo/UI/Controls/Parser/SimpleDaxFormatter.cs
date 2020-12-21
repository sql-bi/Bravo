using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Media;

namespace Sqlbi.Bravo.UI.Controls.Parser
{
    public static class SimpleDaxFormatter
    {
        private const string KeyWordColor = "Purple";
        private const string FunctionColor = "Blue";
        private const string ParenthesisColor = "Red";

        public static List<Inline> FormatLine(string line)
        {
            var result = new List<Inline>();

            var parsed = SimpleDaxParser.ParseLine(line);

            foreach (var (text, textType) in parsed)
            {
                switch (textType)
                {
                    case ParsedTextType.PlainText:
                        result.Add(new Run(text));
                        break;

                    case ParsedTextType.Keyword:
                        result.Add(new Run(text) { Foreground = new SolidColorBrush(Colors.Purple) });
                        break;

                    case ParsedTextType.Function:
                        result.Add(new Run(text) { Foreground = new SolidColorBrush(Colors.Red) });
                        break;

                    case ParsedTextType.Parenthesis:
                        result.Add(new Run(text) { Foreground = new SolidColorBrush(Colors.Blue) });
                        break;

                    default:
                        break;
                }
            }

            return result;
        }
    }
}
