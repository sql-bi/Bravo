﻿namespace Sqlbi.Bravo.Services
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
            BravoUnexpectedException.ThrowIfNull(options.AutoLineBreakStyle);
            BravoUnexpectedException.ThrowIfNull(options.LineBreakStyle);

            var daxformatterResponses = await CallDaxFormatterAsync(measures, options).ConfigureAwait(false);
            var formattedMeasures = new List<FormattedMeasure>();
            {
                var lineBreakStyle = options.LineBreakStyle == DaxLineBreakStyle.Auto
                    ? options.AutoLineBreakStyle
                    : options.LineBreakStyle;

                foreach (var (daxformatterResponse, index) in daxformatterResponses.WithIndex())
                {
                    var requestedMeasure = measures.ElementAt(index);
                    var formattedMeasure = new FormattedMeasure
                    {
                        ETag = requestedMeasure.ETag,
                        Name = requestedMeasure.Name,
                        TableName = requestedMeasure.TableName,
                    };

                    if (daxformatterResponse.Errors.Count == 0)
                    {
                        formattedMeasure.Expression = daxformatterResponse.Formatted;
                        formattedMeasure.LineBreakStyle = lineBreakStyle.Value;
                    }
                    else
                    {
                        formattedMeasure.Expression = requestedMeasure.Expression; // in case of errors returns the original expression, as requested by Daniele
                        formattedMeasure.Errors = daxformatterResponse.Errors?.Select((e) => new FormatterError
                        {
                            Line = e.Line,
                            Column = e.Column,
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
            // TODO: move add/remove FormatPrefix to the Dax.Formatter NugGet package
            const string FormatPrefix = "[x] := ";
            const int FormatPrefixLength = 7;
            
            var request = new DaxFormatterMultipleRequest
            {
                CallerApp = AppEnvironment.ApplicationName,
                CallerVersion = AppEnvironment.ApplicationProductVersion,
                MaxLineLength = options.LineStyle,
                SkipSpaceAfterFunctionName = options.SpacingStyle,
            };

            // TODO: set DaxFormatterRequest ListSeparator and DecimalSeparator nullable
            request.ListSeparator = options.ListSeparator.GetValueOrDefault(request.ListSeparator);
            request.DecimalSeparator = options.DecimalSeparator.GetValueOrDefault(request.DecimalSeparator);

            foreach (var measure in measures)
            {
                request.Dax.Add(FormatPrefix + measure.Expression);
            }

            var responses = await _daxformatterClient.FormatAsync(request).ConfigureAwait(false);

            foreach (var response in responses)
            {
                if (response.Errors.Count == 0)
                {
                    response.Formatted = response.Formatted?.Remove(0, FormatPrefixLength - 1);
                    response.Formatted = response.Formatted?.NormalizeDax().Expression;
                }
                else
                {
                    foreach (var error in response.Errors)
                    {
                        if (error.Line == 0)
                        {
                            // subtract the length of the prefix we removed only if the error is on the first line (zero-based index)
                            error.Column -= FormatPrefixLength;
                            // Don't 'break;' because we can have multilple errors on the first line
                        }
                    }
                }
            }

            return responses;
        }
    }
}