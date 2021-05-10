using Sqlbi.Bravo.UI.Framework.ViewModels;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class NavigationItem : BaseViewModel
    {
        public string Name { get; set; }

        public string Glyph { get; set; }

        public object IconControl { get; set; }

        public SubPage SubPageInTab { get; set; }

        public bool ShowComingSoon { get; set; }

        public bool IsSignInItem { get; set; }

        public bool IsEnabled => !ShowComingSoon;
    }
}
