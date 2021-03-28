using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Sqlbi.Bravo.Client.PowerBI.PowerBICloud;
using Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services
{
    internal class PowerBICloudService : IPowerBICloudService
    {
        private readonly TimeSpan LoginTimeout = TimeSpan.FromMinutes(1);

        private readonly ILogger _logger;
        private readonly PowerBICloudManager _manager;
        private AuthenticationResult _authenticationResult;
        private CancellationTokenSource _loginCancellationTokenSource;

        public PowerBICloudService(ILogger<PowerBIDesktopService> logger)
        {
            _logger = logger;

            _logger.Trace();
            _manager = new PowerBICloudManager();
        }

        public IAccount Account => _authenticationResult?.Account;

        public async Task<bool> LoginAsync()
        {
            // TODO: synchronize access
            _loginCancellationTokenSource = new CancellationTokenSource();
            _loginCancellationTokenSource.CancelAfter(LoginTimeout);
            
            try
            {
                _authenticationResult = await PowerBICloudManager.AcquireTokenAsync(_authenticationResult?.Account, _loginCancellationTokenSource.Token);

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            _authenticationResult = null;

            await PowerBICloudManager.RemoveTokenAsync();
        }

        public async Task<IEnumerable<MetadataSharedDataset>> GetSharedDatasetsAsync()
        {
            return await _manager.GetSharedDatasetsAsync();
        }
    }
}