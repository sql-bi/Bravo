using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Models;
using Sqlbi.Bravo.Services;
using System.Collections.Generic;
using System.Net.Mime;

namespace Sqlbi.Bravo.Controllers
{
    [Route("api/[action]")]
    [ApiController]
    public class AnalyzeModelController : ControllerBase
    {
        private readonly IAnalyzeModelService _analyzeModelService;
        private readonly IPBIDesktopService _pbidesktopService;

        public AnalyzeModelController(IAnalyzeModelService analyzeModelService, IPBIDesktopService pbidesktopService)
        {
            _analyzeModelService = analyzeModelService;
            _pbidesktopService = pbidesktopService;
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
