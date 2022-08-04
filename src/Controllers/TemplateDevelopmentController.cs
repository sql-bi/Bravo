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
        internal static string ControllerPathSegment = "/TemplateDevelopment";

        private readonly ITemplateDevelopmentService _templateDevelopmentService;
        private readonly IAnalyzeModelService _analyzeModelService;

        public TemplateDevelopmentController(ITemplateDevelopmentService templateDevelopmentService, IAnalyzeModelService analyzeModelService)
        {
            _templateDevelopmentService = templateDevelopmentService;
            _analyzeModelService = analyzeModelService;
        }

        /// <summary>
        /// Returns all the <see cref="CustomPackage"/> of type <see cref="CustomPackageType.Organization"/> from the organization repository
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="403">Status403Forbidden -  Use of the tremplate development API is not enabled</response>
        [HttpGet]
        [ActionName("GetOrganizationCustomPackages")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CustomPackage>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesDefaultResponseType]
        public IActionResult GetOrganizationCustomPackages(CancellationToken cancellationToken)
        {
            if (_templateDevelopmentService.Enabled)
            {
                // TODO: retreive CustomPackageType.Organization packages
                var packages = System.Array.Empty<CustomPackage>();
                return Ok(packages);
            }

            return Forbid();
        }

        /// <summary>
        /// Displays a dialog box that prompts the user to select a '.package.json' file or 'code-workspace' file and returns the <see cref="CustomPackage"/>
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        /// <response code="403">Status403Forbidden -  Use of the tremplate development API is not enabled</response>
        [HttpGet]
        [ActionName("BrowseCustomPackage")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomPackage))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesDefaultResponseType]
        public IActionResult BrowseCustomPackage(CancellationToken cancellationToken)
        {
            if (_templateDevelopmentService.Enabled)
            {
                if (WindowDialogHelper.OpenFileDialog(filter: "Bravo template files (*.package.json, *.code-workspace)|*.package.json;*.code-workspace", out var path, cancellationToken))
                {
                    var customPackage = _templateDevelopmentService.GetCustomPackage(path, CustomPackageType.User);
                    return Ok(customPackage);
                }

                return NoContent();
            }

            return Forbid();
        }

        /// <summary>
        /// Gets all the available <see cref="DateConfiguration"/> from the embedded templates
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="403">Status403Forbidden -  Use of the tremplate development API is not enabled</response>
        [HttpGet]
        [ActionName("GetConfigurations")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DateConfiguration>))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesDefaultResponseType]
        public IActionResult GetConfigurations()
        {
            if (_templateDevelopmentService.Enabled)
            {
                var configurations = _templateDevelopmentService.GetConfigurations();
                return Ok(configurations);
            }

            return Forbid();
        }

        /// <summary>
        /// Validate the <see cref="CustomPackage"/> by verifying the existence of the .code-workspace and package.json files
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="403">Status403Forbidden -  Use of the tremplate development API is not enabled</response>
        [HttpGet]
        [ActionName("ValidateCustomPackage")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomPackage))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesDefaultResponseType]
        public IActionResult ValidateCustomPackage(CustomPackage customPackage, CancellationToken cancellationToken)
        {
            if (_templateDevelopmentService.Enabled)
            {
                var validatedCustomPackage = _templateDevelopmentService.ValidateCustomPackage(customPackage);
                return Ok(validatedCustomPackage);
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomPackage))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesDefaultResponseType]
        public IActionResult CreateWorkspace(CreateWorkspaceRequest request, CancellationToken cancellationToken)
        {
            if (_templateDevelopmentService.Enabled)
            {
                if (WindowDialogHelper.BrowseFolderDialog(out var path, cancellationToken))
                {
                    var customPackage = _templateDevelopmentService.CreateWorkspace(path, request.Name!, request.Configuration!);
                    return Ok(customPackage);
                }

                return NoContent();
            }

            return Forbid();
        }

        /// <summary>
        /// Configure an existing template development workspace by updating the bravo-config.json file
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="403">Status403Forbidden - Use of the template development API is not enabled</response>
        /// <response code="403">Status404NotFound - The selected folder does not contain the Bravo workspace configuration file</response>
        [HttpPost]
        [ActionName("ConfigureWorkspace")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult ConfigureWorkspace(string workspacePath, bool openCodeWorkspace, CancellationToken cancellationToken)
        {
            if (_templateDevelopmentService.Enabled)
            {
                if (_templateDevelopmentService.ConfigureWorkspace(workspacePath, openCodeWorkspace))
                    return Ok();

                return NotFound();
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
            if (_templateDevelopmentService.Enabled)
            {
                if (ProcessHelper.OpenShellExecute(path, waitForStarted, out var processId, cancellationToken))
                {
                    var report = PBIDesktopReport.CreateFrom(processId.Value);
                    return Ok(report);
                }
            }

            return Forbid();
        }

        /// <summary>
        /// Returns a list of all open <see cref="PBIDesktopReport"/>
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpGet]
        [ActionName("GetReports")]
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
        [ActionName("GetModel")]
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
        /// Applies the provided <see cref="CustomPackage"/> file without commit changes and returns a preview of changes to objects and data
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        [HttpPost]
        [ActionName("GetPreviewChanges")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dax.Template.Model.ModelChanges))]
        [ProducesDefaultResponseType]
        public IActionResult GetPreviewChanges(WorkspacePreviewChangesRequest request, CancellationToken cancellationToken)
        {
            if (_templateDevelopmentService.Enabled)
            {
                var modelChanges = _templateDevelopmentService.GetPreviewChanges(request.Report!, request.Settings!, cancellationToken);
                return Ok(modelChanges);
            }

            return Forbid();
        }
    }
}
