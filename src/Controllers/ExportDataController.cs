namespace Sqlbi.Bravo.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Services;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Export data controller
    /// </summary>
    /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
    [Route("api/[action]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public class ExportDataController : ControllerBase
    {
        private readonly IExportDataService _exportDataService;
        private readonly IPBICloudService _pbicloudService;

        public ExportDataController(IExportDataService exportDataService, IPBICloudService pbicloudService)
        {
            _exportDataService = exportDataService;
            _pbicloudService = pbicloudService;
        }

        /// <summary>
        /// Exports tables from a <see cref="PBIDesktopReport"/> using the provided <see cref="ExportDelimitedTextSettings"/> format settings 
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. a 'Cancel' button has been pressed on a dialog box)</response>
        [HttpPost]
        [ActionName("ExportCsvFromReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportDataJob))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult ExportDelimitedTextFileFromPBIDesktopReport(ExportDelimitedTextFromPBIReportRequest request, CancellationToken cancellationToken)
        {
            var (canceled, path) = WindowDialogHelper.BrowseFolderDialog(cancellationToken);
            if (canceled)
                return NoContent();

            request.Settings!.ExportPath = path!;
            var job = _exportDataService.ExportDelimitedTextFile(request.Report!, request.Settings!, cancellationToken);

            return Ok(job);
        }

        /// <summary>
        /// Exports tables from a <see cref="PBICloudDataset"/> using the provided <see cref="ExportDelimitedTextSettings"/> format settings 
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. a 'Cancel' button has been pressed on a dialog box)</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpPost]
        [ActionName("ExportCsvFromDataset")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportDataJob))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ExportDelimitedTextFileFromPBICloudDataset(ExportDelimitedTextFromPBICloudDatasetRequest request, CancellationToken cancellationToken)
        {
            if (await _pbicloudService.IsSignInRequiredAsync())
                return Unauthorized();

            var (canceled, path) = WindowDialogHelper.BrowseFolderDialog(cancellationToken);
            if (canceled)
                return NoContent();

            request.Settings!.ExportPath = path!;
            var job = _exportDataService.ExportDelimitedTextFile(request.Dataset!, request.Settings!, cancellationToken);
            
            return Ok(job);
        }

        /// <summary>
        /// Exports tables from a <see cref="PBIDesktopReport"/> using the provided <see cref="ExportExcelSettings"/> format settings 
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. a 'Cancel' button has been pressed on a dialog box)</response>
        [HttpPost]
        [ActionName("ExportXlsxFromReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportDataJob))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public IActionResult ExportExcelFileFromPBIDesktopReport(ExportExcelFromPBIReportRequest request, CancellationToken cancellationToken)
        {
            var (canceled, path) = WindowDialogHelper.SaveFileDialog(fileName: request.Report!.ReportName, defaultExt: "XLSX", cancellationToken);
            if (canceled)
                return NoContent();

            request.Settings!.ExportPath = path!;
            var job = _exportDataService.ExportExcelFile(request.Report, request.Settings, cancellationToken);

            return Ok(job);
        }

        /// <summary>
        /// Exports tables from a <see cref="PBICloudDataset"/> using the provided <see cref="ExportExcelSettings"/> format settings 
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="204">Status204NoContent - User canceled action (e.g. a 'Cancel' button has been pressed on a dialog box)</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpPost]
        [ActionName("ExporXlsxFromDataset")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportDataJob))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ExportExcelFileFromPBICloudDataset(ExportExcelFromPBICloudDatasetRequest request, CancellationToken cancellationToken)
        {
            if (await _pbicloudService.IsSignInRequiredAsync())
                return Unauthorized();

            var (canceled, path) = WindowDialogHelper.SaveFileDialog(fileName: request.Dataset!.DisplayName, defaultExt: "XLSX", cancellationToken);
            if (canceled)
                return NoContent();

            request.Settings!.ExportPath = path!;
            var job = _exportDataService.ExportExcelFile(request.Dataset, request.Settings, cancellationToken);

            return Ok(job);
        }

        /// <summary>
        /// Returns the details of a <see cref="PBIDesktopReport"/> export job to allow monitoring of ongoing activity
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="404">Status404NotFound - Export job not found</response>
        [HttpPost]
        [ActionName("QueryExportFromReport")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportDataJob))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult QueryExportFromReport(PBIDesktopReport report)
        {
            var job = _exportDataService.QueryExportJob(report);
            if (job is null)
                return NotFound();

            return Ok(job);
        }

        /// <summary>
        /// Returns the details of a <see cref="PBICloudDataset"/> export job to allow monitoring of ongoing activity
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="404">Status404NotFound - Export job not found</response>
        [HttpPost]
        [ActionName("QueryExportFromDataset")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportDataJob))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult QueryExportFromDataset(PBICloudDataset dataset)
        {
            var job = _exportDataService.QueryExportJob(dataset);
            if (job is null)
                return NotFound();

            return Ok(job);
        }
    }
}
