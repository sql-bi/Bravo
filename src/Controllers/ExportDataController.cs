namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Windows;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.ExportData;
    using Sqlbi.Bravo.Services;
    using System.IO;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    /// <summary>
    /// ExportData module controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("api/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class ExportDataController : ControllerBase
    {
        private readonly IExportDataService _exportDataService;
        private readonly IAuthenticationService _authenticationService;

        public ExportDataController(IExportDataService exportDataService, IAuthenticationService authenticationService)
        {
            _exportDataService = exportDataService;
            _authenticationService = authenticationService;
        }

        /// <summary>
        /// Exports tables from a <see cref="PBIDesktopReport"/> using the provided <see cref="ExportDelimitedTextSettings"/> format settings 
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        [HttpPost]
        [ActionName("ExportCsvFromReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportDataJob))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult ExportDelimitedTextFile(ExportDelimitedTextFromPBIReportRequest request, CancellationToken cancellationToken)
        {
            if (WindowDialogHelper.BrowseFolderDialog(out var path, cancellationToken))
            {
                if (request.Settings!.CreateSubfolder)
                {
                    if (!GetExportSubfolderPath(ref path, name: request.Report!.ReportName))
                        return NoContent();
                }

                var job = _exportDataService.ExportDelimitedTextFile(request.Report!, request.Settings!, path, cancellationToken);
                return Ok(job);
            }
            
            return NoContent();
        }

        /// <summary>
        /// Exports tables from a <see cref="PBICloudDataset"/> using the provided <see cref="ExportDelimitedTextSettings"/> format settings 
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpPost]
        [ActionName("ExportCsvFromDataset")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportDataJob))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ExportDelimitedTextFile(ExportDelimitedTextFromPBICloudDatasetRequest request, CancellationToken cancellationToken)
        {
            if (await _authenticationService.IsPBICloudSignInRequiredAsync(cancellationToken))
                return Unauthorized();

            if (WindowDialogHelper.BrowseFolderDialog(out var path, cancellationToken))
            {
                if (request.Settings!.CreateSubfolder)
                {
                    if (!GetExportSubfolderPath(ref path, name: request.Dataset!.DisplayName))
                        return NoContent();
                }

                var job = _exportDataService.ExportDelimitedTextFile(request.Dataset!, request.Settings!, path, _authenticationService.PBICloudAuthentication.AccessToken, cancellationToken);
                return Ok(job);
            }

            return NoContent();
        }

        /// <summary>
        /// Exports tables from a <see cref="PBIDesktopReport"/> using the provided <see cref="ExportExcelSettings"/> format settings 
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. 'Cancel' button has been pressed on a dialog box)</response>
        [HttpPost]
        [ActionName("ExportXlsxFromReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportDataJob))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult ExportExcelFile(ExportExcelFromPBIReportRequest request, CancellationToken cancellationToken)
        {
            var useZip64 = CommonHelper.IsKeyDown(System.Windows.Forms.Keys.ControlKey);

            if (WindowDialogHelper.SaveFileDialog(fileName: request.Report!.ReportName, filter: null, defaultExt: "XLSX", out var path, cancellationToken))
            {
                var job = _exportDataService.ExportExcelFile(request.Report, request.Settings!, path, useZip64, cancellationToken);
                return Ok(job);
            }

            return NoContent();
        }

        /// <summary>
        /// Exports tables from a <see cref="PBICloudDataset"/> using the provided <see cref="ExportExcelSettings"/> format settings 
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. a 'Cancel' button has been pressed on a dialog box)</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpPost]
        [ActionName("ExportXlsxFromDataset")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportDataJob))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ExportExcelFile(ExportExcelFromPBICloudDatasetRequest request, CancellationToken cancellationToken)
        {
            var useZip64 = CommonHelper.IsKeyDown(System.Windows.Forms.Keys.ControlKey);

            if (await _authenticationService.IsPBICloudSignInRequiredAsync(cancellationToken))
                return Unauthorized();

            if (WindowDialogHelper.SaveFileDialog(fileName: request.Dataset!.DisplayName, filter: null, defaultExt: "XLSX", out var path, cancellationToken))
            {
                var job = _exportDataService.ExportExcelFile(request.Dataset, request.Settings!, path, _authenticationService.PBICloudAuthentication.AccessToken, useZip64, cancellationToken);
                return Ok(job);
            }

            return NoContent();
        }

        /// <summary>
        /// Returns the details of a <see cref="PBIDesktopReport"/> export job to allow monitoring of ongoing activity
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - Export job not available for querying</response>
        [HttpPost]
        [ActionName("QueryExportFromReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportDataJob))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult QueryExportJob(PBIDesktopReport report)
        {
            var job = _exportDataService.QueryExportJob(report);

            if (job is null)
                return NoContent();

            return Ok(job);
        }

        /// <summary>
        /// Returns the details of a <see cref="PBICloudDataset"/> export job to allow monitoring of ongoing activity
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - Export job not available for querying</response>
        [HttpPost]
        [ActionName("QueryExportFromDataset")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportDataJob))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult QueryExportJob(PBICloudDataset dataset)
        {
            var job = _exportDataService.QueryExportJob(dataset);

            if (job is null)
                return NoContent();

            return Ok(job);
        }

        private static bool GetExportSubfolderPath(ref string path, string? name)
        {
            if (name.IsNullOrWhiteSpace())
            {
                name = "New Folder";
            }

            var subfolderName = name.ReplaceInvalidPathChars();
            var subfolderPath = Path.Combine(path, subfolderName);

            if (Directory.Exists(subfolderPath))
            {
                var overwriteButton = new TaskDialogCommandLinkButton("&Overwrite", "Overwrite and replace files in the destination folder");
                var keepbothButton = new TaskDialogCommandLinkButton("&Keep Both", "Files will be exported to a new folder");
                var cancelButton = new TaskDialogCommandLinkButton("&Cancel", "Cancel export");
                var heading = $"The destination folder you chose already contains a subfolder named '{ subfolderName }'";
                var text = "Choose an option to proceed";

                var clickedButton = MessageDialog.ShowDialog(heading, text, footnoteText: null, allowCancel: true, overwriteButton, keepbothButton, cancelButton);

                if (clickedButton == overwriteButton)
                {
                    //
                }
                else if (clickedButton == keepbothButton)
                {
                    for (var i = 1; /**/ ; i++)
                    {
                        var uniquePath = Path.Combine(path, $"{ subfolderName } - { i }");

                        if (!Directory.Exists(uniquePath))
                        {
                            subfolderPath = uniquePath;
                            break;
                        }
                    }
                }
                else
                {
                    path = string.Empty;
                    return false;
                }
            }

            path = subfolderPath;
            return true;
        }
    }
}
