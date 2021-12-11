using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IPBICloudAuthenticationService _authenticationService;

        public AnalyzeModelController(IAnalyzeModelService analyzeModelService, IPBIDesktopService pbidesktopService, IPBICloudService pbicloudService, IPBICloudAuthenticationService authenticationService)
        {
            _analyzeModelService = analyzeModelService;
            _pbidesktopService = pbidesktopService;
            _pbicloudService = pbicloudService;
            _authenticationService = authenticationService;
        }

        /// <summary>
        /// Returns a database model from the VPAX file stream
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
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
        /// <response code="200">Status200OK - Success</response>
        /// <response code="404">Status404NotFound - The requested PBIDesktop instance is no longer running</response>
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
        /// <response code="200">Status200OK - Success</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpGet]
        [ActionName("ListDatasets")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PBICloudDataset>))]
        public async Task<IActionResult> GetPBICloudDatasets()
        {
            var accessToken = _authenticationService.CurrentAuthentication?.AccessToken;
            if (accessToken is null)
                return Unauthorized();

            var onlineWorkspaces = await _pbicloudService.GetWorkspacesAsync(accessToken).ConfigureAwait(false);
            var onlineDatasets = await _pbicloudService.GetSharedDatasetsAsync(accessToken).ConfigureAwait(false);

            var selectedWorkspaces = onlineWorkspaces.Where((w) => w.CapacitySkuType == WorkspaceCapacitySkuType.Premium);
            var selectedDatasets = onlineDatasets.Where((d) => !d.Model.IsExcelWorkbook /* && !d.Model.IsPushDataEnabled */); // TOFIX: exclude datasets where IsPushDataEnabled

            var datasets = selectedDatasets.Join(selectedWorkspaces, (d) => d.WorkspaceObjectId, (w) => w.Id, resultSelector: (d, w) => new PBICloudDataset
            {
                WorkspaceId = d.WorkspaceId,
                WorkspaceName = d.WorkspaceName,
                Id = d.Model.Id,
                DisplayName = d.Model.DisplayName,
                Description = d.Model.Description,
                Owner = $"{ d.Model.CreatorUser.GivenName } { d.Model.CreatorUser.FamilyName }",
                Refreshed = d.Model.LastRefreshTime,
                Endorsement = (PBICloudDatasetEndorsement)(d.GalleryItem?.Stage ?? (int)PBICloudDatasetEndorsement.None)
            },
            StringComparer.InvariantCultureIgnoreCase);

            return Ok(datasets);
        }

        /// <summary>
        /// Returns a list of all open PBIDesktop reports
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
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
        /// <response code="200">Status200OK - Success</response>
        /// <response code="404">Status404NotFound - The requested PBIDesktop instance is no longer running</response>
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
