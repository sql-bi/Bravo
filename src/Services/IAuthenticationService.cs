using Microsoft.Identity.Client;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Services
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> AcquireTokenAsync(IAccount account);

        Task ClearTokenCache();
    }
}