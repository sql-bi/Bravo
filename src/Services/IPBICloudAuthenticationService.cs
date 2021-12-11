using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Services
{
    public interface IPBICloudAuthenticationService
    {
        AuthenticationResult? CurrentAuthentication { get; }

        Task AcquireTokenAsync(TimeSpan cancelAfter, string? identifier = default);

        Task ClearTokenCacheAsync();
    }
}