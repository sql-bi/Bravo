﻿namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.AnalyzeModel;
    using Sqlbi.Bravo.Services;

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
        private const string VpaxObfuscatedFileFilter = "VPAX obfuscated file (*.ovpax)|*.ovpax";
        private const string VpaxFileFilter = "VPAX file (*.vpax)|*.vpax";

        private readonly IAnalyzeModelService _analyzeModelService;
        private readonly IAuthenticationService _authenticationService;

        public AnalyzeModelController(IAnalyzeModelService analyzeModelService, IAuthenticationService authenticationService)
        {
            _analyzeModelService = analyzeModelService;
            _authenticationService = authenticationService;
        }

        /// <summary>
        /// Returns a deobfuscated database model from the OVPAX file stream
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        [HttpPost]
        [ActionName("GetModelFromOvpax")]
        [Consumes(MediaTypeNames.Application.Octet)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TabularDatabase))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult GetDeobfuscatedDatabase(CancellationToken cancellationToken)
        {
            if (!WindowDialogHelper.OpenFileDialog(filter: VpaxObfuscationDictionaryFilter, out var deobfuscationDictionaryPath, cancellationToken))
                return NoContent();

            var database = _analyzeModelService.GetDatabase(stream: Request.Body, deobfuscationDictionaryPath);
            return Ok(database);
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
        public IActionResult GetDatabase(CancellationToken cancellationToken)
        {
            var database = _analyzeModelService.GetDatabase(stream: Request.Body);
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
        public IActionResult ExportVpax([FromBody] PBIDesktopReport report, [FromQuery] VpaxObfuscationMode mode, CancellationToken cancellationToken)
        {
            if (!TryGetExportPaths(report.ReportName, mode, out var path, out var obfuscationDictionaryPath, out var obfuscationIncrementalDictionaryPath, cancellationToken))
                return NoContent();

            _analyzeModelService.ExportVpax(report, path, obfuscationDictionaryPath, obfuscationIncrementalDictionaryPath, cancellationToken);
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
        public async Task<IActionResult> ExportVpax([FromBody] PBICloudDataset dataset, [FromQuery] VpaxObfuscationMode mode, CancellationToken cancellationToken)
        {
            if (await _authenticationService.IsPBICloudSignInRequiredAsync(cancellationToken))
                return Unauthorized();

            if (!TryGetExportPaths(dataset.DisplayName, mode, out var path, out var obfuscationDictionaryPath, out var obfuscationIncrementalDictionaryPath, cancellationToken))
                return NoContent();

            _analyzeModelService.ExportVpax(dataset, _authenticationService.PBICloudAuthentication.AccessToken, path, obfuscationDictionaryPath, obfuscationIncrementalDictionaryPath, cancellationToken);
            return Ok();
        }

        private bool TryGetExportPaths(string? fileName, VpaxObfuscationMode mode, [NotNullWhen(true)] out string? path, out string? obfuscationDictionaryPath, out string? obfuscationIncrementalDictionaryPath, CancellationToken cancellationToken)
        {
            path = obfuscationDictionaryPath = obfuscationIncrementalDictionaryPath = null;

            switch (mode)
            {
                case VpaxObfuscationMode.None:
                    {
                        if (!WindowDialogHelper.SaveFileDialog(fileName, filter: VpaxFileFilter, defaultExt: "VPAX", out path, cancellationToken))
                            return false;
                    }
                    break;
                case VpaxObfuscationMode.Default:
                    {
                        if (!WindowDialogHelper.SaveFileDialog(fileName, filter: VpaxObfuscatedFileFilter, defaultExt: "OVPAX", out path, cancellationToken))
                            return false;
                        if (!WindowDialogHelper.SaveFileDialog(fileName, filter: VpaxObfuscationDictionaryFilter, defaultExt: "DICT", out obfuscationDictionaryPath, cancellationToken))
                            return false;
                    }
                    break;
                case VpaxObfuscationMode.Incremental:
                    {
                        if (!WindowDialogHelper.OpenFileDialog(filter: VpaxObfuscationDictionaryFilter, out obfuscationIncrementalDictionaryPath, cancellationToken))
                            return false;

                        goto case VpaxObfuscationMode.Default;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, message: "Invalid obfuscation mode");
            }

            return true;
        }
    }
}
