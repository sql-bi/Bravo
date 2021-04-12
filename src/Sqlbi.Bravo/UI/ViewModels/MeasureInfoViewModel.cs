using Sqlbi.Bravo.Client.DaxFormatter.Interfaces;
using Sqlbi.Bravo.UI.Framework.ViewModels;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class MeasureInfoViewModel : BaseViewModel
    {
        public string Name { get; set; }

        public string OriginalDax { get; set; }

        public string FormatterDax { get; set; }

        public bool Reformat { get; set; } = true;

        public ITabularObject TabularObject { get; set; }

        public bool IsAlreadyFormatted => OriginalDax.Trim() == FormatterDax.Trim();
    }
}
