using Bravo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        /// Returns a database model from the VPAX file stream.
        /// </summary>
        [HttpPost]
        [ActionName("GetModelFromVpax")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(DatabaseModel), StatusCodes.Status200OK)]
        public IActionResult GetDatabaseModelFromVpax()
        {
            var databaseModel = _analyzeModelService.GetDatabaseModelFromVpax(stream: Request.Body);
            
            return Ok(databaseModel);
        }

        /// <summary>
        /// Returns a database model from a PBIDesktop instance.
        /// </summary>
        /// <response code="404">The PBIDesktop instance requested is no longer running.</response>
        [HttpPost]
        [ActionName("GetModelFromReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(DatabaseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetDatabaseModelFromPBIDesktop(PBIDesktopModel model)
        {
            var pbidesktop = _pbidesktopService.GetInstanceDetails(instance: model);
            if (pbidesktop is null)
                return NotFound();

            // TODO: set default values for TomExtractor readStatisticsFromData/sampleRows properties
            var databaseModel = _analyzeModelService.GetDatabaseModelFromSSAS(pbidesktop, readStatisticsFromData: default, sampleRows: default);

            return Ok(databaseModel);
        }

        /// <summary>
        /// Returns a list of all active PBIDesktop instances.
        /// </summary>
        [HttpGet]
        [ActionName("ListReports")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<PBIDesktopModel>), StatusCodes.Status200OK)]
        public IActionResult GetPBIDesktopInstances()
        {
            var pbidesktopModel = _pbidesktopService.GetActiveInstances();

            return Ok(pbidesktopModel);
        }
    }
}
