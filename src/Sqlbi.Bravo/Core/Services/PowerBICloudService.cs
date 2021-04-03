using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Sqlbi.Bravo.Client.PowerBI.PowerBICloud;
using Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services
{
    internal class PowerBICloudService : IPowerBICloudService
    {
        private readonly ILogger _logger;
        private AuthenticationResult _authenticationResult;

        public PowerBICloudService(ILogger<PowerBIDesktopService> logger)
        {
            _logger = logger;

            _logger.Trace();
        }

        public bool IsAuthenticated => _authenticationResult != null;

        public IAccount Account => _authenticationResult?.Account;

        public async Task<bool> LoginAsync()
        {
            _logger.Trace();

            try
            {
                _authenticationResult = await PowerBICloudManager.AcquireTokenAsync(_authenticationResult?.Account);

                return true;
            }
            catch (MsalException) // ex.ErrorCode => Microsoft.Identity.Client.MsalError
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            _logger.Trace();

            _authenticationResult = null;

            await PowerBICloudManager.RemoveTokenAsync();
        }

        public async Task<IEnumerable<MetadataSharedDataset>> GetSharedDatasetsAsync()
        {
            _logger.Trace();

            return await PowerBICloudManager.GetSharedDatasetsAsync(_authenticationResult.AccessToken);
        }
    }
}