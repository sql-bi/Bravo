using Microsoft.Identity.Client;
using Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IPowerBICloudService
    {
        public IAccount Account { get; }

        public TimeSpan LoginTimeout { get; }

        Task<bool> LoginAsync(Action callback, CancellationToken cancellationToken);

        Task LogoutAsync();

        Task<IEnumerable<MetadataSharedDataset>> GetSharedDatasetsAsync();
    }
}