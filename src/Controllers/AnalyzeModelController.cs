using Bravo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Services;
using System.Collections.Generic;
using System.Net.Mime;

namespace Sqlbi.Bravo.Controllers
{
    [Route("api/AnalyzeModel/[action]")]
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
        /// Returns a list of active PowerBI desktop instances containing local Power BI reports
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
