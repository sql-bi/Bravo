using Dax.Formatter;
using Dax.Formatter.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Models;
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

        public FormatDaxController(IDaxFormatterClient daxformatterClient)
        {
            _daxformatterClient = daxformatterClient;
        }

        /// <summary>
        /// Format the provided DAX measures by using daxformatter.com service
        /// </summary>
        /// <response code="200">Status200OK - Success</response>
        /// <response code="404">Status404BadRequest - Required parameters are missing</response>
        [HttpPost]
        [ActionName("FormatDax")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FormatDaxResponse))]
        public async Task<IActionResult> FormatDax(FormatDaxRequest request)
        {
            if (request.Options is null || request.Measures is null)
                return BadRequest();

            var daxformatterResponse = await CallDaxFormatter(request.Measures, request.Options).ConfigureAwait(false);

            var response = new FormatDaxResponse();

            foreach (var (daxformatterMeasure, index) in daxformatterResponse.WithIndex())
            {
                var formatterResult = new FormatterResult
                {
                    Expression = daxformatterMeasure.Formatted?.Remove(0, $"[{ request.Measures.ElementAt(index).Name }] :=".Length)?.TrimStart('\r', '\n', ' ')?.TrimEnd('\r', '\n', ' '),
                    Errors = daxformatterMeasure.Errors?.Select((e) => new FormatterError
                    {
                        Line = e.Line,
                        Column = e.Column,
                        Message = e.Message
                    })
                };

                response.Add(formatterResult);
            }

            return Ok(response);
        }

        private Task<IReadOnlyList<DaxFormatterResponse>> CallDaxFormatter(IEnumerable<TabularMeasure> measures, FormatDaxOptions options)
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
            {
                request.Dax.Add($"[{ measure.Name }] := { measure.Expression }");
            }

            return _daxformatterClient.FormatAsync(request);
        }
    }
}
