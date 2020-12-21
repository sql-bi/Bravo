using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Media;

namespace Sqlbi.Bravo.UI.Controls.Parser
{
    public static class SimpleDaxFormatter
    {
        private static readonly Color KeyWordColor = Color.FromRgb(3, 90, 202); // "#035ACA";
        private static readonly Color FunctionColor = Color.FromRgb(3, 90, 202); // "#035ACA";
        private static readonly Color ParenthesisColor = Color.FromRgb(128, 128, 128); // "#808080";

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
                        result.Add(new Run(text) { Foreground = new SolidColorBrush(KeyWordColor) });
                        break;

                    case ParsedTextType.Function:
                        result.Add(new Run(text) { Foreground = new SolidColorBrush(FunctionColor) });
                        break;

                    case ParsedTextType.Parenthesis:
                        result.Add(new Run(text) { Foreground = new SolidColorBrush(ParenthesisColor) });
                        break;

                    default:
                        break;
                }
            }

            return result;
        }
    }
}
