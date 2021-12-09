using Bravo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Services;
using System.Collections.Generic;
using System.IO;
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
        /// <response code="200">Success.</response>
        [HttpPost]
        [ActionName("GetModelFromVpax")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DatabaseModel))]
        public IActionResult GetDatabaseModelFromVpax()
        {
            var databaseModel = _analyzeModelService.GetDatabaseModelFromVpax(stream: Request.Body);
            
            return Ok(databaseModel);
        }

        /// <summary>
        /// Returns a database model from a PBIDesktop instance.
        /// </summary>
        /// <response code="200">Success.</response>
        /// <response code="404">The requested PBIDesktop instance is no longer running.</response>
        [HttpPost]
        [ActionName("GetModelFromReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DatabaseModel))]
        public IActionResult GetDatabaseModelFromPBIDesktop(PBIDesktopModel model)
        {
            var pbidesktop = _pbidesktopService.GetInstanceDetails(pbidesktop: model);
            if (pbidesktop is null)
                return NotFound();

            // TODO: set default values for TomExtractor readStatisticsFromData/sampleRows properties
            var databaseModel = _analyzeModelService.GetDatabaseModelFromSSAS(pbidesktop, readStatisticsFromData: default, sampleRows: default);

            return Ok(databaseModel);
        }

        /// <summary>
        /// Returns a list of all active PBIDesktop instances.
        /// </summary>
        /// <response code="200">Success.</response>
        [HttpGet]
        [ActionName("ListReports")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PBIDesktopModel>))]
        public IActionResult GetPBIDesktopInstances()
        {
            var pbidesktopModel = _pbidesktopService.GetActiveInstances();

            return Ok(pbidesktopModel);
        }

        /// <summary>
        /// Returns a VPAX file stream from a PBIDesktop instance.
        /// </summary>
        /// <response code="200">Success.</response>
        /// <response code="404">The requested PBIDesktop instance is no longer running.</response>
        [HttpPost]
        [ActionName("ExportVpaxFromReport")]
        [Produces(MediaTypeNames.Application.Octet)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(byte[]))]
        public IActionResult GetVpaxFromPBIDesktop(PBIDesktopModel model)
        {
            var vpaxStream = _pbidesktopService.ExportVpax(model);

            return Ok(vpaxStream);
        }
    }
}
