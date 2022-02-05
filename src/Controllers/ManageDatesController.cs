namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Models.ManageDates;
    using Sqlbi.Bravo.Services;
    using System.Collections.Generic;
    using System.Net.Mime;

    /// <summary>
    /// ManageDates module controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("api/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class ManageDatesController : ControllerBase
    {
        private readonly IManageDatesService _manageDatesService;

        public ManageDatesController(IManageDatesService manageDatesService)
        {
            _manageDatesService = manageDatesService;
        }

        /// <summary>
        /// ??
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("GetDateConfigurations")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DateConfiguration>))]
        [ProducesDefaultResponseType]
        public IActionResult GetConfigurations()
        {
            var configurations = _manageDatesService.GetConfigurations();

            return Ok(configurations);
        }

        /// <summary>
        /// ??
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("ApplyDateConfiguration")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult ApplyConfiguration(DateConfiguration configuration)
        {
            var modelChanges = _manageDatesService.Apply(configuration, commitChanges: false, previewRows: 5);

            return Ok(modelChanges);
        }
    }
}
