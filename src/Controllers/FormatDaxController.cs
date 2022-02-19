namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Models.FormatDax;
    using Sqlbi.Bravo.Services;
    using System.Net.Mime;
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
        private readonly IPBIDesktopService _pbidesktopService;
        private readonly IPBICloudService _pbicloudService;

        public FormatDaxController(IFormatDaxService formatDaxService, IPBIDesktopService pbidesktopService, IPBICloudService pbicloudService)
        {
            _formatDaxService = formatDaxService;
            _pbidesktopService = pbidesktopService;
            _pbicloudService = pbicloudService;
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
        public async Task<IActionResult> FormatAsync(FormatDaxRequest request)
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
        /// Update a PBIDesktop report by applying changes to formatted measures
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("UpdateReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DatabaseUpdateResult))]
        [ProducesDefaultResponseType]
        public IActionResult UpdatePBIDesktopReportAsync(UpdatePBIDesktopReportRequest request)
        {
            var databaseETag = _pbidesktopService.Update(request.Report!, request.Measures!);
            var updateResult = new DatabaseUpdateResult
            {
                DatabaseETag = databaseETag
            };

            return Ok(updateResult);
        }

        /// <summary>
        /// Update a PBICloud dataset by applying changes to formatted measures 
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
        public async Task<IActionResult> UpdatePBICloudDatasetAsync(UpdatePBICloudDatasetRequest request)
        {
            if (await _pbicloudService.IsSignInRequiredAsync())
                return Unauthorized();

            var databaseETag = _pbicloudService.Update(request.Dataset!, request.Measures!);
            var updateResult = new DatabaseUpdateResult
            {
                DatabaseETag = databaseETag
            };

            return Ok(updateResult);
        }
    }
}
