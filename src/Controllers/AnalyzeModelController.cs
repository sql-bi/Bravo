using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
using Sqlbi.Bravo.Models;
using Sqlbi.Bravo.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Controllers
{
    [Route("api/[action]")]
    [ApiController]
    public class AnalyzeModelController : ControllerBase
    {
        private readonly IPBIDesktopService _pbidesktopService;
        private readonly IPBICloudService _pbicloudService;

        public AnalyzeModelController(IPBIDesktopService pbidesktopService, IPBICloudService pbicloudService)
        {
            _pbidesktopService = pbidesktopService;
            _pbicloudService = pbicloudService;
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
            var database = VpaxToolsHelper.GetDatabaseFromVpax(stream: Request.Body);
            return Ok(database);
        }

        /// <summary>
        /// Returns a database model from a PBIDesktop instance
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="404">Status404NotFound - PBIDesktop report not found</response>
        [HttpPost]
        [ActionName("GetModelFromReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        public IActionResult GetDatabaseFromPBIDesktopReport(PBIDesktopReport report)
        {
            Stream stream;
            try
            {
                stream = _pbidesktopService.ExportVpax(report, includeTomModel: false, includeVpaModel: false, readStatisticsFromData: false, sampleRows: 0);
            }
            catch (TOMDatabaseNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            var database = VpaxToolsHelper.GetDatabaseFromVpax(stream);
            return Ok(database);
        }

        /// <summary>
        /// Returns a database model from a PBICloud dataset
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        /// <response code="404">Status404NotFound - PBICloud dataset not found</response>
        [HttpPost]
        [ActionName("GetModelFromDataset")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        public IActionResult GetDatabaseFromPBICloudDataset(PBICloudDataset dataset)
        {
            if (_pbicloudService.IsAuthenticated == false)
                return Unauthorized();

            Stream stream;
            try
            {
                stream = _pbicloudService.ExportVpax(dataset, includeTomModel: false, includeVpaModel: false, readStatisticsFromData: false, sampleRows: 0);
            }
            catch (TOMDatabaseNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            var database = VpaxToolsHelper.GetDatabaseFromVpax(stream);
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
            if (_pbicloudService.IsAuthenticated == false)
                return Unauthorized();

            var onlineWorkspaces = await _pbicloudService.GetWorkspacesAsync();
            var onlineDatasets = await _pbicloudService.GetSharedDatasetsAsync();

            var selectedWorkspaces = onlineWorkspaces.Where((w) => w.CapacitySkuType == WorkspaceCapacitySkuType.Premium);
            // TOFIX: exclude unsupported datasets https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools#unsupported-datasets
            var selectedDatasets = onlineDatasets.Where((d) => !d.Model.IsExcelWorkbook /* && !d.Model.IsPushDataEnabled */);

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
        /// <response code="404">Status404NotFound - PBIDesktop report not found</response>
        [HttpPost]
        [ActionName("ExportVpaxFromReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Octet)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(byte[]))]
        public IActionResult GetVpaxFromPBIDesktopReport(PBIDesktopReport report)
        {
            Stream vpax;
            try
            {
                vpax = _pbidesktopService.ExportVpax(report, includeTomModel: false, includeVpaModel: false, readStatisticsFromData: false, sampleRows: 0);
            }
            catch (TOMDatabaseNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            
            return Ok(vpax);
        }

        /// <summary>
        /// Returns a VPAX file stream from a PBICloud dataset
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        /// <response code="404">Status404NotFound - PBICloud dataset not found</response>
        [HttpPost]
        [ActionName("ExportVpaxFromDataset")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Octet)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(byte[]))]
        public IActionResult GetVpaxFromPBICloudDataset(PBICloudDataset dataset)
        {
            if (_pbicloudService.IsAuthenticated == false)
                return Unauthorized();

            Stream vpax;
            try
            {
                vpax = _pbicloudService.ExportVpax(dataset, includeTomModel: false, includeVpaModel: false, readStatisticsFromData: false, sampleRows: 0);
            }
            catch (TOMDatabaseNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            return Ok(vpax);
        }
    }
}
