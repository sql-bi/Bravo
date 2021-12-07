using Bravo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Services;
using System.Net.Mime;

namespace Sqlbi.Bravo.Controllers
{
    [Route("api/AnalyzeModel/[action]")]
    [ApiController]
    public class AnalyzeModelController : ControllerBase
    {
        private readonly IAnalyzeModelService _analyzeModelService;

        public AnalyzeModelController(IAnalyzeModelService analyzeModelService)
        {
            _analyzeModelService = analyzeModelService;
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
    }
}
