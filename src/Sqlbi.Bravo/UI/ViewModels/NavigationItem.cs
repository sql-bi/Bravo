using System;

namespace Sqlbi.Bravo.UI.ViewModels
{
    public class NavigationItem
    {
        public string Name { get; set; }

        public string Glyph { get; set; }

        public Type NavigationPage { get; set; }

        public bool ShowComingSoon { get; set; }

        public bool IsEnabled => !ShowComingSoon;
    }
}
