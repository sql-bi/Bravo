using Dax.Formatter;
using Dax.Formatter.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Models;
using Sqlbi.Bravo.Services;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Controllers
{
    [Route("api/[action]")]
    [ApiController]
    public class FormatDaxController : ControllerBase
    {
        private readonly IDaxFormatterClient _daxformatterClient;
        private readonly IPBIDesktopService _pbidesktopService;
        private readonly IPBICloudService _pbicloudService;

        public FormatDaxController(IDaxFormatterClient daxformatterClient, IPBIDesktopService pbidesktopService, IPBICloudService pbicloudService)
        {
            _daxformatterClient = daxformatterClient;
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
            var daxformatterResponse = await CallDaxFormatterAsync(request.Measures!, request.Options!).ConfigureAwait(false);
            var response = new FormatDaxResponse();

            foreach (var (daxformatterMeasure, index) in daxformatterResponse.WithIndex())
            {
                var requestedMeasure = request.Measures!.ElementAt(index);
                var formattedMeasure = new FormattedMeasure
                {
                    ETag = requestedMeasure.ETag,
                    Name = requestedMeasure.Name,
                    TableName = requestedMeasure.TableName,
                };

                var daxformatterExpressionPrefixLength = $"[{ requestedMeasure.Name }] :=".Length;

                if (daxformatterMeasure.Errors.Count == 0)
                {
                    formattedMeasure.Expression = daxformatterMeasure.Formatted?.Remove(0, daxformatterExpressionPrefixLength)?.TrimStart('\r', '\n', ' ')?.TrimEnd('\r', '\n', ' ');
                }
                else
                {
                    formattedMeasure.Expression = requestedMeasure.Expression; // in case of errors returns the original expression, as requested by Daniele
                    formattedMeasure.Errors = daxformatterMeasure.Errors?.Select((e) => new FormatterError
                    {
                        Line = e.Line,
                        Column = e.Column - (e.Line == 0 ? daxformatterExpressionPrefixLength : 0), // remove prefix only if the error is on the first line (zero-based index)
                        Message = e.Message
                    });
                }

                response.Add(formattedMeasure);
            }

            return Ok(response);

            async Task<IReadOnlyList<DaxFormatterResponse>> CallDaxFormatterAsync(IEnumerable<TabularMeasure> measures, FormatDaxOptions options)
            {
                var request = new DaxFormatterMultipleRequest
                {
                    CallerApp = AppConstants.ApplicationName,
                    CallerVersion = AppConstants.ApplicationFileVersion,
                    MaxLineLength = options.LineStyle,
                    SkipSpaceAfterFunctionName = options.SpacingStyle,
                };

                // TODO : set DaxFormatterRequest.ListSeparator nullable
                request.ListSeparator = options.ListSeparator.GetValueOrDefault(request.ListSeparator);
                // TODO : set DaxFormatterRequest.DecimalSeparator nullable
                request.DecimalSeparator = options.DecimalSeparator.GetValueOrDefault(request.DecimalSeparator);

                foreach (var measure in measures)
                    request.Dax.Add($"[{ measure.Name }] := { measure.Expression }");

                var response = await _daxformatterClient.FormatAsync(request).ConfigureAwait(false);
                return response;
            }
        }

        /// <summary>
        /// Update a PBIDesktop report by applying changes to formatted measures
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
        [HttpPost]
        [ActionName("UpdateReport")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DatabaseUpdateResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public IActionResult UpdatePBIDesktopReportAsync(UpdatePBIDesktopReportRequest request)
        {
            try
            {
                var databaseETag = _pbidesktopService.Update(request.Report!, request.Measures!);

                return Ok(new DatabaseUpdateResult
                {
                    DatabaseETag = databaseETag
                });
            }
            catch (TOMDatabaseException ex)
            {
                return Problem(ex.ProblemDetail, ex.ProblemInstance, StatusCodes.Status400BadRequest);
            }
        }

        /// <summary>
        /// Update a PBICloud dataset by applying changes to formatted measures 
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="400">Status400BadRequest - See the "instance" and "detail" properties to identify the specific occurrence of the problem</response>
        /// <response code="401">Status401Unauthorized - Sign-in required</response>
        [HttpPost]
        [ActionName("UpdateDataset")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DatabaseUpdateResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> UpdatePBICloudDatasetAsync(UpdatePBICloudDatasetRequest request)
        {
            if (await _pbicloudService.IsSignInRequiredAsync())
                return Unauthorized();

            try
            {
                var databaseETag = _pbicloudService.Update(request.Dataset!, request.Measures!);

                return Ok(new DatabaseUpdateResult
                {
                    DatabaseETag = databaseETag
                });
            }
            catch (TOMDatabaseException ex)
            {
                return Problem(ex.ProblemDetail, ex.ProblemInstance, StatusCodes.Status400BadRequest);
            }
        }
    }
}
