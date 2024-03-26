namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.AnalyzeModel;
    using Sqlbi.Bravo.Services;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// AnalyzeModel module controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("api/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class AnalyzeModelController : ControllerBase
    {
        private const string VpaxObfuscationDictionaryFilter = "VPAX obfuscation dictionary (*.dict)|*.dict";
        private const string VpaxObfuscatedFilter = "VPAX obfuscated file (*.ovpax)|*.ovpax";
        private const string VpaxFilter = "VPAX file (*.vpax)|*.vpax";

        private readonly IAnalyzeModelService _analyzeModelService;
        private readonly IAuthenticationService _authenticationService;

        public AnalyzeModelController(IAnalyzeModelService analyzeModelService, IAuthenticationService authenticationService)
        {
            _analyzeModelService = analyzeModelService;
            _authenticationService = authenticationService;
        }

        /// <summary>
        /// Returns a database model from the VPAX file stream
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        [HttpPost]
        [ActionName("GetModelFromVpax")]
        [Consumes(MediaTypeNames.Application.Octet)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult GetDatabase(CancellationToken cancellationToken) // @daniele: remove the whole method
        {
            string? dictionaryPath = null;

            var deobfuscate = CommonHelper.IsKeyDown(System.Windows.Forms.Keys.ShiftKey);
            if (deobfuscate && !WindowDialogHelper.OpenFileDialog(filter: VpaxObfuscationDictionaryFilter, out dictionaryPath, cancellationToken))
                return NoContent();

            using var dictionaryStream = dictionaryPath != null ? new System.IO.FileStream(dictionaryPath, System.IO.FileMode.Open, System.IO.FileAccess.Read) : null;
            var database = _analyzeModelService.GetDatabase(stream: Request.Body, dictionaryStream);
            return Ok(database);
        }

        /// <summary>
        /// Returns a database model from the VPAX file stream. If an obfuscation dictionary is provided, the model will be deobfuscated.
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetModelFromVpax_NEW")] // @daniele: rename removing the '_NEW' suffix
        //[Consumes(MediaTypeNames.Application.Octet)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        [ProducesDefaultResponseType]
        public IActionResult GetDatabase(IFormFile[] files)
        {
            using var stream = files[0].OpenReadStream();
            using var dictionaryStream = files.ElementAtOrDefault(1)?.OpenReadStream();

            var database = _analyzeModelService.GetDatabase(stream, dictionaryStream);
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
        /// Prompts the user to select a location for saving a VPAX file generated from an active <see cref="PBIDesktopReport"/>
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        [HttpPost]
        [ActionName("ExportVpaxFromReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult ExportVpax(PBIDesktopReport report, ExportVpaxMode mode, CancellationToken cancellationToken)
        {
            // @daniele: remove IF block
            if (mode == ExportVpaxMode.Default)
            {
                if (CommonHelper.IsKeyDown(System.Windows.Forms.Keys.ShiftKey)) mode = ExportVpaxMode.Obfuscate; else
                if (CommonHelper.IsKeyDown(System.Windows.Forms.Keys.ControlKey)) mode = ExportVpaxMode.ObfuscateIncremental;
            }

            if (!TryGetExportPaths(report.ReportName, mode, out var path, out var dictionaryPath, out var inputDictionaryPath, cancellationToken))
                return NoContent();

            _analyzeModelService.ExportVpax(report, path, dictionaryPath, inputDictionaryPath, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Prompts the user to select a location for saving a VPAX file generated from an active <see cref="PBICloudDataset"/>
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpPost]
        [ActionName("ExportVpaxFromDataset")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ExportVpax(PBICloudDataset dataset, ExportVpaxMode mode, CancellationToken cancellationToken)
        {
            if (mode == ExportVpaxMode.Default) // @daniele: remove IF block
            {
                if (CommonHelper.IsKeyDown(System.Windows.Forms.Keys.ShiftKey)) mode = ExportVpaxMode.Obfuscate; else
                if (CommonHelper.IsKeyDown(System.Windows.Forms.Keys.ControlKey)) mode = ExportVpaxMode.ObfuscateIncremental;
            }

            if (await _authenticationService.IsPBICloudSignInRequiredAsync(cancellationToken))
                return Unauthorized();

            if (!TryGetExportPaths(dataset.DisplayName, mode, out var path, out var dictionaryPath, out var inputDictionaryPath, cancellationToken))
                return NoContent();

            _analyzeModelService.ExportVpax(dataset, path, dictionaryPath, inputDictionaryPath, _authenticationService.PBICloudAuthentication.AccessToken, cancellationToken);
            return Ok();
        }

        private static bool TryGetExportPaths(string? fileName, ExportVpaxMode mode, [NotNullWhen(true)] out string? path, out string? dictionaryPath, out string? inputDictionaryPath, CancellationToken cancellationToken)
        {
            path = null;
            dictionaryPath = null;
            inputDictionaryPath = null;

            if (mode == ExportVpaxMode.ObfuscateIncremental && !WindowDialogHelper.OpenFileDialog(filter: VpaxObfuscationDictionaryFilter, out inputDictionaryPath, cancellationToken))
                return false;

            if (!WindowDialogHelper.SaveFileDialog(fileName, filter: mode.IsObfuscate() ? VpaxObfuscatedFilter : VpaxFilter, defaultExt: mode.IsObfuscate() ? "OVPAX" : "VPAX", out path, cancellationToken))
                return false;

            if (mode.IsObfuscate() && !WindowDialogHelper.SaveFileDialog(fileName, filter: VpaxObfuscationDictionaryFilter, defaultExt: "DICT", out dictionaryPath, cancellationToken))
                return false;

            return true;
        }
    }
}
