using Sqlbi.Bravo.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Sqlbi.Bravo.UI.Framework.TemplateSelector
{
    public class MenuItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GlyphDataTemplate { get; set; }

        public DataTemplate PathDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is NavigationItem navItem)
            {
                return string.IsNullOrWhiteSpace(navItem.Glyph)
                    ? PathDataTemplate
                    : GlyphDataTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
