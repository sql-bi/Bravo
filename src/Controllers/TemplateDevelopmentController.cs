namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models.ManageDates;
    using Sqlbi.Bravo.Services;
    using System.Net.Mime;
    using System.Threading;

    /// <summary>
    /// TemplateDevelopment module controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("TemplateDevelopment/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class TemplateDevelopmentController : ControllerBase
    {
        private readonly ITemplateDevelopmentService _templateDevelopmentService;

        public TemplateDevelopmentController(ITemplateDevelopmentService templateDevelopmentService)
        {
            _templateDevelopmentService = templateDevelopmentService;
        }

        /// <summary>
        /// Create and initialize a new tremplate development workspace by cloning an existing template from the provided <see cref="DateConfiguration"/>
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        [HttpPost]
        [ActionName("CreateWorkspace")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult CreateWorkspace(DateConfiguration configuration, CancellationToken cancellationToken)
        {
            if (WindowDialogHelper.BrowseFolderDialog(out var path, cancellationToken))
            {
                _templateDevelopmentService.CreateWorkspace(path, configuration, cancellationToken);
                return Ok();
            }

            return NoContent();
        }
    }
}
