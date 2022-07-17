namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.AnalyzeModel;
    using Sqlbi.Bravo.Models.ManageDates;
    using Sqlbi.Bravo.Models.TemplateDevelopment;
    using Sqlbi.Bravo.Services;
    using System.Collections.Generic;
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
        internal static string ControllerName = "TemplateDevelopment";

        private readonly ITemplateDevelopmentService _templateDevelopmentService;
        private readonly IAnalyzeModelService _analyzeModelService;

        public TemplateDevelopmentController(ITemplateDevelopmentService templateDevelopmentService, IAnalyzeModelService analyzeModelService)
        {
            _templateDevelopmentService = templateDevelopmentService;
            _analyzeModelService = analyzeModelService;
        }

        /// <summary>
        /// Enable or disable the use of the tremplate development APIs
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("UpdateStatus")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public IActionResult UpdateStatus(bool enabled)
        {
            _templateDevelopmentService.UpdateStatus(enabled);
            return Ok();
        }

        /// <summary>
        /// Gets all the available <see cref="DateConfiguration"/> from the embedded templates
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="403">Status403Forbidden -  Use of the tremplate development API is not enabled</response>
        [HttpPost]
        [ActionName("GetTemplateConfigurations")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DateConfiguration>))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesDefaultResponseType]
        public IActionResult GetTemplateConfigurations()
        {
            if (_templateDevelopmentService.Enabled)
            {
                var configurations = _templateDevelopmentService.GetTemplateConfigurations();
                return Ok(configurations);
            }

            return Forbid();
        }

        /// <summary>
        /// Create and initialize a new template development workspace by cloning an existing template from the provided <see cref="DateConfiguration"/>
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        /// <response code="403">Status403Forbidden -  Use of the tremplate development API is not enabled</response>
        [HttpPost]
        [ActionName("CreateWorkspace")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesDefaultResponseType]
        public IActionResult CreateWorkspace(CreateWorkspaceRequest request)
        {
            if (_templateDevelopmentService.Enabled)
            {
                if (WindowDialogHelper.BrowseFolderDialog(out var path, CancellationToken.None))
                {
                    _templateDevelopmentService.CreateWorkspace(path, request.Name!, request.Configuration!);
                    return Ok();
                }

                return NoContent();
            }

            return Forbid();
        }

        /// <summary>
        /// Configure an existing template development workspace by updating the bravo-config.json file/>
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        /// <response code="403">Status403Forbidden -  Use of the tremplate development API is not enabled</response>
        [HttpPost]
        [ActionName("ConfigureWorkspace")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesDefaultResponseType]
        public IActionResult ConfigureWorkspace()
        {
            if (_templateDevelopmentService.Enabled)
            {
                if (WindowDialogHelper.BrowseFolderDialog(out var path, CancellationToken.None))
                {
                    _templateDevelopmentService.ConfigureWorkspace(path);
                    return Ok();
                }

                return NoContent();
            }

            return Forbid();
        }

        /// <summary>
        /// Launches the Power BI Desktop process after displaying a dialog box that prompts the user to select the PBIX file to be opened
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        /// <response code="403">Status403Forbidden - The path is invalid or not allowed</response>
        [HttpGet]
        [ActionName("PBIDesktopOpenPBIX")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PBIDesktopReport))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult PBIDesktopOpenPBIX(string path, bool waitForStarted, CancellationToken cancellationToken)
        {
            if (ProcessHelper.OpenShellExecute(path, waitForStarted, out var processId, cancellationToken))
            {
                var report = PBIDesktopReport.CreateFrom(processId.Value);
                return Ok(report);
            }

            return Forbid();
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
            if (_templateDevelopmentService.Enabled)
            {
                var reports = _analyzeModelService.GetReports(cancellationToken);
                return Ok(reports);
            }

            return Forbid();
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
            if (_templateDevelopmentService.Enabled)
            {
                var database = _analyzeModelService.GetDatabase(report, cancellationToken);
                return Ok(database);
            }

            return Forbid();
        }

        /// <summary>
        /// Applies the provided settings without commit changes and returns a preview of changes to objects and data
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetPreviewChanges")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dax.Template.Model.ModelChanges))]
        [ProducesDefaultResponseType]
        public IActionResult GetPreviewChanges(WorkspacePreviewChangesRequest request, CancellationToken cancellationToken)
        {
            var modelChanges = _templateDevelopmentService.GetPreviewChanges(request.Report!, request.Settings!, cancellationToken);
            return Ok(modelChanges);
        }
    }
}
