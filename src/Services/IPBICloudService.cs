using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Services
{
    public interface IPBICloudService
    {
        Task<IEnumerable<Workspace>> GetWorkspacesAsync(string accessToken);

        Task<IEnumerable<SharedDataset>> GetSharedDatasetsAsync(string accessToken);
    }
}