using Microsoft.Extensions.DependencyInjection;
using Sqlbi.Bravo.UI.ViewModels;

namespace Sqlbi.Bravo.UI.Framework.ViewModels
{
    internal class ViewModelLocator
    {
        public ShellViewModel ShellViewModel => App.ServiceProvider.GetRequiredService<ShellViewModel>();

        public SideMenuViewModel SideMenuViewModel => App.ServiceProvider.GetRequiredService<SideMenuViewModel>();

        public DaxFormatterViewModel DaxFormatterViewModel => App.ServiceProvider.GetRequiredService<DaxFormatterViewModel>();

        public SettingsViewModel SettingsViewModel => App.ServiceProvider.GetRequiredService<SettingsViewModel>();
    }
}
