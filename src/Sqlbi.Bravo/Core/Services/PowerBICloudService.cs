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
        private readonly ILogger _logger;
        private AuthenticationResult _authenticationResult;

        public TimeSpan LoginTimeout { get; } = TimeSpan.FromSeconds(30);

        public PowerBICloudService(ILogger<PowerBIDesktopService> logger)
        {
            _logger = logger;

            _logger.Trace();
        }

        public IAccount Account => _authenticationResult?.Account;

        public async Task<bool> LoginWithCustomUIAsync()
        {
            _logger.Trace();

            try
            {
                _authenticationResult = await PowerBICloudManager.AcquireTokenAsync(_authenticationResult?.Account,CancellationToken.None, useCustomLoginUI: true);

                return true;
            }
            catch (MsalException ex)
            {
                _ = System.Windows.MessageBox.Show($"MsalException ==> { ex.Message }", "DEBUG", System.Windows.MessageBoxButton.OK);

                return false;
            }

        }
        
        public async Task<bool> LoginWithSystemBrowserAsync(Action callback, CancellationToken cancellationToken)
        {
            _logger.Trace();

            // TODO: synchronize access
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenSource.CancelAfter(LoginTimeout);

            try
            {
                _authenticationResult = await PowerBICloudManager.AcquireTokenAsync(_authenticationResult?.Account, cancellationTokenSource.Token);

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                callback?.Invoke();
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