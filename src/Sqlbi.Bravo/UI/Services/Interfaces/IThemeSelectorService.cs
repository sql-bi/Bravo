using Sqlbi.Bravo.UI.DataModel;

namespace Sqlbi.Bravo.UI.Services.Interfaces
{
    public interface IThemeSelectorService
    {
        void InitializeTheme();

        void SetTheme(AppTheme theme);

        AppTheme GetCurrentTheme();
    }
}
