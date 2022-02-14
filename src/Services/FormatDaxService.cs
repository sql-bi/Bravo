namespace Sqlbi.Bravo.Services
{
    using Dax.Formatter;
    using Dax.Formatter.Models;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Models.AnalyzeModel;
    using Sqlbi.Bravo.Models.FormatDax;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public interface IFormatDaxService
    {
        Task<IEnumerable<FormattedMeasure>> FormatAsync(IEnumerable<TabularMeasure> measures, FormatDaxOptions options);
    }

    internal class FormatDaxService : IFormatDaxService
    {
        private readonly IDaxFormatterClient _daxformatterClient;

        public FormatDaxService(IDaxFormatterClient daxformatterClient)
        {
            _daxformatterClient = daxformatterClient;
        }

        public async Task<IEnumerable<FormattedMeasure>> FormatAsync(IEnumerable<TabularMeasure> measures, FormatDaxOptions options)
        {
            var daxformatterResponses = await CallDaxFormatterAsync(measures, options).ConfigureAwait(false);

            var formattedMeasures = new List<FormattedMeasure>();
            {
                foreach (var (daxformatterResponse, index) in daxformatterResponses.WithIndex())
                {
                    var requestedMeasure = measures.ElementAt(index);
                    var formattedMeasure = new FormattedMeasure
                    {
                        ETag = requestedMeasure.ETag,
                        Name = requestedMeasure.Name,
                        TableName = requestedMeasure.TableName,
                    };

                    var daxformatterExpressionPrefixLength = $"[{ requestedMeasure.Name }] :=".Length;

                    if (daxformatterResponse.Errors.Count == 0)
                    {
                        formattedMeasure.Expression = daxformatterResponse.Formatted?.Remove(0, daxformatterExpressionPrefixLength)?.TrimStart('\n', ' ')?.TrimEnd('\n', ' ');
                    }
                    else
                    {
                        formattedMeasure.Expression = requestedMeasure.Expression; // in case of errors returns the original expression, as requested by Daniele
                        formattedMeasure.Errors = daxformatterResponse.Errors?.Select((e) => new FormatterError
                        {
                            Line = e.Line,
                            Column = e.Column - (e.Line == 0 ? daxformatterExpressionPrefixLength : 0), // subtract prefix length only if the error is on the first line (zero-based index)
                            Message = e.Message
                        });
                    }

                    formattedMeasures.Add(formattedMeasure);
                }
            }

            return formattedMeasures;
        }

        private async Task<IReadOnlyList<DaxFormatterResponse>> CallDaxFormatterAsync(IEnumerable<TabularMeasure> measures, FormatDaxOptions options)
        {
            var request = new DaxFormatterMultipleRequest
            {
                CallerApp = AppEnvironment.ApplicationName,
                CallerVersion = AppEnvironment.ApplicationProductVersion,
                MaxLineLength = options.LineStyle,
                SkipSpaceAfterFunctionName = options.SpacingStyle,
            };

            // TODO : set DaxFormatterRequest.ListSeparator nullable
            request.ListSeparator = options.ListSeparator.GetValueOrDefault(request.ListSeparator);
            // TODO : set DaxFormatterRequest.DecimalSeparator nullable
            request.DecimalSeparator = options.DecimalSeparator.GetValueOrDefault(request.DecimalSeparator);

            foreach (var measure in measures)
                request.Dax.Add($"[{ measure.Name }] := { measure.Expression }");

            var responses = await _daxformatterClient.FormatAsync(request).ConfigureAwait(false);

            foreach (var response in responses)
            {
                // TODO: (HACK) daxformatter.com service applies CRLF as EOL character while SSAS uses LF.
                response.Formatted = response.Formatted?.Replace("\r\n", "\n");
            }

            return responses;
        }
    }
}
