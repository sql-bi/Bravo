using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.Framework.ViewModels;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class ApplicationViewModel : BaseViewModel
    {
        public static ApplicationViewModel Instance = new ApplicationViewModel();

        public ApplicationView CurrentView { get; set; } = ApplicationView.None;
    }
}
