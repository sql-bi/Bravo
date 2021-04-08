using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Sqlbi.Bravo.Client.PowerBI;
using Sqlbi.Bravo.Client.PowerBI.PowerBICloud;
using Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public string AccessToken => _authenticationResult?.AccessToken;

        public PowerBICloudEnvironment CloudEnvironment => PowerBICloudManager.CloudEnvironment;

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

        public async Task<IEnumerable<PowerBICloudSharedDataset>> GetDatasetsAsync()
        {
            _logger.Trace();

            var workspaces = await PowerBICloudManager.GetWorkspacesAsync(_authenticationResult.AccessToken);
            var datasets = await PowerBICloudManager.GetSharedDatasetsAsync(_authenticationResult.AccessToken);

            var premiumWorkspaces = workspaces.Where((w) => WorkspaceCapacitySkuType.Premium.Equals(w.GetWorkspaceCapacitySkuType()));
            var cloudWorkspaces = datasets.Where((d) => !d.Model.IsExcelWorkbook && !d.Model.IsPushDataEnabled)
                .Join(premiumWorkspaces, (d) => d.WorkspaceObjectId.ToUpperInvariant(), (w) => w.Id.ToUpperInvariant(), (d, w) => new
                {
                    Workspace = w,
                    Dataset = d
                })
                .Select((a) => new PowerBICloudSharedDataset
                {
                    WorkspaceId = Guid.Parse(a.Workspace.Id),
                    WorkspaceName = a.Workspace.Name,
                    WorkspaceType = a.Workspace.GetWorkspaceType(),
                    WorkspaceCapacitySkuType = a.Workspace.GetWorkspaceCapacitySkuType(),
                    Permissions = a.Dataset.Permissions,
                    Model = a.Dataset.Model
                })
                .ToArray();

            return cloudWorkspaces;
        }
    }
}