using Sqlbi.Bravo.UI.DataModel;

namespace Sqlbi.Bravo.UI.Services.Interfaces
{
    public interface IThemeSelectorService
    {
        void InitializeTheme(string themeName);

        void SetTheme(string themeName);
    }
}
