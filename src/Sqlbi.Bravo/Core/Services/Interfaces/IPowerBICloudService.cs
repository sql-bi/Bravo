using Microsoft.Identity.Client;
using Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IPowerBICloudService
    {
        public bool IsAuthenticated { get; }

        public IAccount Account { get; }

        Task<bool> LoginAsync();

        Task LogoutAsync();

        Task<IEnumerable<MetadataSharedDataset>> GetSharedDatasetsAsync();
    }
}