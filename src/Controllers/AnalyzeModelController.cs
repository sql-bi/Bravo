namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.AnalyzeModel;
    using Sqlbi.Bravo.Services;
    using System.Windows.Forms;

    /// <summary>
    /// AnalyzeModel module controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("api/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class AnalyzeModelController : ControllerBase
    {
        private readonly IAnalyzeModelService _analyzeModelService;
        private readonly IAuthenticationService _authenticationService;
        private readonly SaveFileDialog _exportVpaxDialog = new()
        {
            Title = "Save VPAX",
            Filter = "VPAX file (*.vpax)|*.vpax|Obfuscated VPAX file (*.ovpax)|*.ovpax",
            DefaultExt = "vpax",
            AddExtension = true,
            OverwritePrompt = true,
            CheckPathExists = true,
            ValidateNames = true
        };

        public AnalyzeModelController(IAnalyzeModelService analyzeModelService, IAuthenticationService authenticationService)
        {
            _analyzeModelService = analyzeModelService;
            _authenticationService = authenticationService;
        }

        /// <summary>
        /// Returns a database model from the VPAX file provided as multipart form data.
        /// An optional obfuscation dictionary file can be included to deobfuscate the VPAX.
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetModelFromVpax")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        [ProducesDefaultResponseType]
        public IActionResult GetDatabase(IFormFile[] files, CancellationToken cancellationToken)
        {
            using var vpaxStream = files[0].OpenReadStream();
            using var obfuscatorDictionaryStream = files.ElementAtOrDefault(1)?.OpenReadStream();

            var database = _analyzeModelService.GetDatabase(vpaxStream, obfuscatorDictionaryStream);
            return Ok(database);
        }

        /// <summary>
        /// Returns a database model from a PBIDesktop instance
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetModelFromReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        [ProducesDefaultResponseType]
        public IActionResult GetDatabase(PBIDesktopReport report, CancellationToken cancellationToken)
        {
            var database = _analyzeModelService.GetDatabase(report, cancellationToken);
            return Ok(database);
        }

        /// <summary>
        /// Returns a database model from a PBICloud dataset
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpPost]
        [ActionName("GetModelFromDataset")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetDatabase(PBICloudDataset dataset, CancellationToken cancellationToken)
        {
            if (await _authenticationService.IsPBICloudSignInRequiredAsync(cancellationToken))
                return Unauthorized();

            var database = _analyzeModelService.GetDatabase(dataset, _authenticationService.PBICloudAuthentication.AccessToken, cancellationToken);
            return Ok(database);
        }

        /// <summary>
        /// Returns a list of all PBICloud datasets
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpGet]
        [ActionName("ListDatasets")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PBICloudDataset>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetDatasets(CancellationToken cancellationToken)
        {
            if (await _authenticationService.IsPBICloudSignInRequiredAsync(cancellationToken))
                return Unauthorized();

            var datasets = await _analyzeModelService.GetDatasetsAsync(cancellationToken);
            return Ok(datasets);
        }

        /// <summary>
        /// Returns a list of all open <see cref="PBIDesktopReport"/>
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("ListReports")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PBIDesktopReport>))]
        [ProducesDefaultResponseType]
        public IActionResult GetReports(CancellationToken cancellationToken)
        {
            var reports = _analyzeModelService.GetReports(cancellationToken);
            return Ok(reports);
        }

        /// <summary>
        /// Returns a list of all open <see cref="PBIDesktopReport"/> reports as fast as possible, providing only process information and without attempting to establish a database connection
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("QueryReports")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PBIDesktopReport>))]
        [ProducesDefaultResponseType]
        public IActionResult QueryReports(CancellationToken cancellationToken)
        {
            var reports = _analyzeModelService.QueryReports(cancellationToken);
            return Ok(reports);
        }

        /// <summary>
        /// Exports the specified Power BI Desktop report to a VPAX file,
        /// allowing the user to select the file location and export mode.
        /// </summary>
        /// <remarks>
        /// The method displays a dialog for the user to choose the destination file
        /// and export mode. The export mode can be either default or obfuscated,
        /// depending on the user's selection.
        /// </remarks>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled the operation</response>
        [HttpPost]
        [ActionName("ExportVpaxFromReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult ExportVpax(PBIDesktopReport report, CancellationToken cancellationToken)
        {
            _exportVpaxDialog.FileName = report.ReportName;

            var dialogResult = _exportVpaxDialog.ShowDialogOnStaThread();
            if (dialogResult != DialogResult.OK)
                return NoContent();

            var path = _exportVpaxDialog.FileName!;
            var mode = _exportVpaxDialog.FilterIndex == 1 ? ExportVpaxMode.Default : ExportVpaxMode.Obfuscated;

            _analyzeModelService.ExportVpax(report, mode, path, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Exports the specified Power BI dataset to a VPAX file,
        /// allowing the user to select the export mode and destination.
        /// </summary>
        /// <remarks>
        /// The method displays a dialog for the user to choose the destination file
        /// and export mode. The export mode can be either default or obfuscated,
        /// depending on the user's selection.
        /// </remarks>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled the operation</response>
        [HttpPost]
        [ActionName("ExportVpaxFromDataset")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ExportVpax(PBICloudDataset dataset, CancellationToken cancellationToken)
        {
            _exportVpaxDialog.FileName = dataset.DisplayName;

            var dialogResult = _exportVpaxDialog.ShowDialogOnStaThread();
            if (dialogResult != DialogResult.OK)
                return NoContent();

            if (await _authenticationService.IsPBICloudSignInRequiredAsync(cancellationToken))
                return Unauthorized();

            var path = _exportVpaxDialog.FileName!;
            var mode = _exportVpaxDialog.FilterIndex == 1 ? ExportVpaxMode.Default : ExportVpaxMode.Obfuscated;
            var accessToken = _authenticationService.PBICloudAuthentication.AccessToken;

            _analyzeModelService.ExportVpax(dataset, accessToken, mode, path, cancellationToken);
            return Ok();
        }
    }
}
