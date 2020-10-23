using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Settings.Interfaces
{
    internal interface IGlobalSettingsProviderService
    {
        IAppSettings Application { get; }

        IRuntimeSettings Runtime { get; }

        Task SaveAsync();
    }
}
