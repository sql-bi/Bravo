using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Settings.Interfaces
{
    internal interface IGlobalSettingsProviderService
    {
        AppSettings Application { get; }

        RuntimeSettings Runtime { get; }

        Task SaveAsync();
    }
}
