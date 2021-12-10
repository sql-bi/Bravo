using Microsoft.Identity.Client;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Services
{
    public interface IPBICloudAuthenticationService
    {
        Task<AuthenticationResult> AcquireTokenAsync(string? identifier = default);

        Task ClearTokenCache();
    }
}