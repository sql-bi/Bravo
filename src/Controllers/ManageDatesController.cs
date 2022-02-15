namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Models.ManageDates;
    using Sqlbi.Bravo.Services;
    using System.Collections.Generic;
    using System.Net.Mime;
    using System.Threading;

    /// <summary>
    /// ManageDates module controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("ManageDates/[action]")]
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
        /// Gets all available template configurations
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("GetConfigurations")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DateConfiguration>))]
        [ProducesDefaultResponseType]
        public IActionResult GetConfigurations()
        {
            var configurations = _manageDatesService.GetConfigurations();

            return Ok(configurations);
        }

        /// <summary>
        /// Applies the provided configuration without commit changes and returns a preview of changes to objects and data
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetPreviewChangesFromReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult GetPreviewChangesFromPBIDesktopReport(PreviewChangesFromPBIDesktopReportRequest request, CancellationToken cancellationToken)
        {
            var modelChanges = _manageDatesService.GetPreviewChanges(request.Report!, request.Settings!, cancellationToken);

            return Ok(modelChanges);
        }

        /// <summary>
        /// Update the model by appliying the provided configuration
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("UpdateReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult UpdatePBIDesktopReport(UpdatePBIDesktopReportRequest request, CancellationToken cancellationToken)
        {
            _manageDatesService.Update(request.Report!, request.Configuration!, cancellationToken);

            return Ok();
        }
    }
}
