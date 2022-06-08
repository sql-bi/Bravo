namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.FormatDax;
    using Sqlbi.Bravo.Services;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// FormatDax module controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("api/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class FormatDaxController : ControllerBase
    {
        private readonly IFormatDaxService _formatDaxService;
        private readonly IAuthenticationService _authenticationService;

        public FormatDaxController(IFormatDaxService formatDaxService, IAuthenticationService authenticationService)
        {
            _formatDaxService = formatDaxService;
            _authenticationService = authenticationService;
        }

        /// <summary>
        /// Format the provided DAX measures by using daxformatter.com service
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("FormatDax")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FormatDaxResponse))]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> Format(FormatDaxRequest request)
        {
            var formattedMeasures = await _formatDaxService.FormatAsync(request.Measures!, request.Options!);

            // TODO: view TODO on FormatDaxResponse class
            //var response = new FormatDaxResponse
            //{
            //    Measures = formattedMeasures,
            //};

            return Ok(formattedMeasures);
        }

        /// <summary>
        /// Update a <see cref="PBIDesktopReport"/> by applying changes to formatted measures
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("UpdateReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DatabaseUpdateResult))]
        [ProducesDefaultResponseType]
        public IActionResult Update(UpdatePBIDesktopReportRequest request)
        {
            var updateResult = _formatDaxService.Update(request.Report!, request.Measures!);
            return Ok(updateResult);
        }

        /// <summary>
        /// Update a <see cref="PBICloudDataset"/> by applying changes to formatted measures 
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpPost]
        [ActionName("UpdateDataset")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DatabaseUpdateResult))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> Update(UpdatePBICloudDatasetRequest request, CancellationToken cancellationToken)
        {
            if (await _authenticationService.IsPBICloudSignInRequiredAsync(cancellationToken))
                return Unauthorized();

            var updateResult = _formatDaxService.Update(request.Dataset!, request.Measures!, _authenticationService.PBICloudAuthentication.AccessToken);
            return Ok(updateResult);
        }
    }
}
