using Microsoft.Identity.Client;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Services
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> AcquireTokenAsync(string identifier);

        Task ClearTokenCache();
    }
}