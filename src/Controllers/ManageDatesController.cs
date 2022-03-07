namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Models;
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
        /// Gets all the available <see cref="DateConfiguration"/> from the embedded templates
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetConfigurationsForReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DateConfiguration>))]
        [ProducesDefaultResponseType]
        public IActionResult GetConfigurations(PBIDesktopReport report, CancellationToken cancellationToken)
        {
            var configurations = _manageDatesService.GetConfigurations(report, cancellationToken);

            return Ok(configurations);
        }

        /// <summary>
        /// Validate the <see cref="DateConfiguration"/> provided against the tabular model
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("ValidateConfigurationForReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DateConfiguration))]
        [ProducesDefaultResponseType]
        public IActionResult ValidateConfiguration(ValidatePBIDesktopReportConfigurationRequest request, CancellationToken cancellationToken)
        {
            var configuration = _manageDatesService.ValidateConfiguration(request.Report!, request.Configuration!, cancellationToken);

            return Ok(configuration);
        }

        /// <summary>
        /// Applies the provided <see cref="DateConfiguration"/> without commit changes and returns a preview of changes to objects and data
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetPreviewChangesFromReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dax.Template.Model.ModelChanges))]
        [ProducesDefaultResponseType]
        public IActionResult GetPreviewChanges(PreviewChangesFromPBIDesktopReportRequest request, CancellationToken cancellationToken)
        {
            var modelChanges = _manageDatesService.GetPreviewChanges(request.Report!, request.Settings!, cancellationToken);

            return Ok(modelChanges);
        }

        /// <summary>
        /// Update the <see cref="PBIDesktopReport"/> by appliying the provided <see cref="DateConfiguration"/>
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("UpdateReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult Update(UpdatePBIDesktopReportRequest request, CancellationToken cancellationToken)
        {
            _manageDatesService.Update(request.Report!, request.Configuration!, cancellationToken);

            return Ok();
        }
    }
}
