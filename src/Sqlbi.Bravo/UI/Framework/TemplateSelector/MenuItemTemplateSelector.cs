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
            return item is NavigationItem navItem
                ? string.IsNullOrWhiteSpace(navItem.Glyph)
                    ? PathDataTemplate
                    : GlyphDataTemplate
                : base.SelectTemplate(item, container);
        }
    }
}
