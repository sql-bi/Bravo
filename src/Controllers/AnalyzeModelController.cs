using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
using Sqlbi.Bravo.Models;
using Sqlbi.Bravo.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Controllers
{
    [Route("api/[action]")]
    [ApiController]
    public class AnalyzeModelController : ControllerBase
    {
        private readonly IAnalyzeModelService _analyzeModelService;
        private readonly IPBIDesktopService _pbidesktopService;
        private readonly IPBICloudService _pbicloudService;
        private readonly IAuthenticationService _authenticationService;

        public AnalyzeModelController(IAnalyzeModelService analyzeModelService, IPBIDesktopService pbidesktopService, IPBICloudService pbicloudService, IAuthenticationService authenticationService)
        {
            _analyzeModelService = analyzeModelService;
            _pbidesktopService = pbidesktopService;
            _pbicloudService = pbicloudService;
            _authenticationService = authenticationService;
        }

        /// <summary>
        /// Returns a database model from the VPAX file stream
        /// </summary>
        /// <response code="200">Success</response>
        [HttpPost]
        [ActionName("GetModelFromVpax")]
        [Consumes(MediaTypeNames.Application.Octet)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        public IActionResult GetDatabaseFromVpax()
        {
            var database = _analyzeModelService.GetDatabaseFromVpax(vpax: Request.Body);
            return Ok(database);
        }

        /// <summary>
        /// Returns a database model from a PBIDesktop instance
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="404">The requested PBIDesktop instance is no longer running</response>
        [HttpPost]
        [ActionName("GetModelFromReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        public IActionResult GetDatabaseFromPBIDesktopReport(PBIDesktopReport report)
        {
            var vpax = _pbidesktopService.ExportVpax(report, includeTomModel: false, includeVpaModel: false);
            if (vpax is null)
                return NotFound();

            var database = _analyzeModelService.GetDatabaseFromVpax(vpax);
            return Ok(database);
        }

        /// <summary>
        /// Returns a list of all PBICloud datasets
        /// </summary>
        /// <response code="200">Success</response>
        [HttpGet]
        [ActionName("ListDatasets")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PBICloudDataset>))]
        public async Task<IActionResult> GetPBICloudDatasets()
        {
            // From IAccount.HomeAccountId.Identifier
            var accountIdentifier = "8b9a3abe-5d5e-4add-ba1d-31f72e2c8994.f545bd66-7c3f-4729-851a-b7ca3ac9fb6e";
            var auth = await _authenticationService.AcquireTokenAsync(accountIdentifier).ConfigureAwait(false);

            var onlineWorkspaces = await _pbicloudService.GetWorkspacesAsync(auth.AccessToken).ConfigureAwait(false);
            var onlineDatasets = await _pbicloudService.GetSharedDatasetsAsync(auth.AccessToken).ConfigureAwait(false);

            var filteredWorkspaces = onlineWorkspaces.Where((w) => w.CapacitySkuType == WorkspaceCapacitySkuType.Premium);
            var filteredDatasets = onlineDatasets.Where((d) => !d.Model.IsExcelWorkbook && !d.Model.IsPushDataEnabled);

            //var cloudDatasets = datasets //.Where((d) => !d.Model.IsExcelWorkbook && !d.Model.IsPushDataEnabled)
            //    .Join(premiumWorkspaces, (d) => d.WorkspaceObjectId.ToUpperInvariant(), (w) => w.Id.ToUpperInvariant(), (d, w) => new PowerBICloudSharedDataset
            //    {
            //        WorkspaceId = Guid.Parse(w.Id),
            //        WorkspaceName = w.Name,
            //        WorkspaceType = w.GetWorkspaceType(),
            //        WorkspaceCapacitySkuType = w.GetWorkspaceCapacitySkuType(),
            //        Permissions = d.Permissions,
            //        Model = d.Model,
            //        GalleryItem = d.GalleryItem
            //    })
            //    .ToArray();

            //var datasets = filteredDatasets.Join(filteredWorkspaces, (d) => d.WorkspaceObjectId, (w) => w.Id, resultSelector: (d, w) => new PBICloudDataset
            //{
            //    WorkspaceId = w.Id,
            //    WorkspaceName = w.Name,
            //},
            //StringComparer.InvariantCultureIgnoreCase);

            var datasets = filteredDatasets.Select((d) => new PBICloudDataset
            {
                WorkspaceId = d.WorkspaceId,
                WorkspaceName = d.WorkspaceName,
                Id = d.Model.Id,
                DisplayName = d.Model.DisplayName,
                Description = d.Model.Description,
                Owner = $"{ d.Model.CreatorUser.GivenName } { d.Model.CreatorUser.FamilyName }",
                Refreshed = d.Model.LastRefreshTime.ToDateTimeOffset(),
                Endorsement = (PBICloudDatasetEndorsement)(d.GalleryItem?.Status ?? (int)PBICloudDatasetEndorsement.None)
            });

            return Ok(datasets);
        }

        /// <summary>
        /// Returns a list of all open PBIDesktop reports
        /// </summary>
        /// <response code="200">Success</response>
        [HttpGet]
        [ActionName("ListReports")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PBIDesktopReport>))]
        public IActionResult GetPBIDesktopReports()
        {
            var reports = _pbidesktopService.GetReports();
            return Ok(reports);
        }

        /// <summary>
        /// Returns a VPAX file stream from an active PBIDesktop report
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="404">The requested PBIDesktop instance is no longer running</response>
        [HttpPost]
        [ActionName("ExportVpaxFromReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json, MediaTypeNames.Application.Octet)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(byte[]))]
        public IActionResult GetVpaxFromPBIDesktopReport(PBIDesktopReport report)
        {
            var vpax = _pbidesktopService.ExportVpax(report);
            if (vpax is null)
                return NotFound();
            
            return Ok(vpax);
        }
    }
}
