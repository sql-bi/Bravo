namespace Sqlbi.Bravo.Services
{
    using Dax.Formatter;
    using Dax.Formatter.Models;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.AnalyzeModel;
    using Sqlbi.Bravo.Models.FormatDax;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public interface IFormatDaxService
    {
        Task<IEnumerable<FormattedMeasure>> FormatAsync(IEnumerable<TabularMeasure> measures, FormatDaxOptions options);

        DatabaseUpdateResult Update(PBIDesktopReport report, IEnumerable<FormattedMeasure> measures);

        DatabaseUpdateResult Update(PBICloudDataset dataset, IEnumerable<FormattedMeasure> measures, string accessToken);
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

                    if (daxformatterResponse.Errors is null || daxformatterResponse.Errors.Count == 0)
                    {
                        formattedMeasure.Expression = daxformatterResponse.Formatted;
                        formattedMeasure.LineBreakStyle = lineBreakStyle.Value;
                    }
                    else if (/* options.IgnoreEmptyExpressionError && */ requestedMeasure.Expression.IsNullOrWhiteSpace()) // TODO: parameterize this behavior (i.e. options.IgnoreEmptyExpressionError)
                    {
                        formattedMeasure.Expression = requestedMeasure.Expression;
                        formattedMeasure.LineBreakStyle = lineBreakStyle.Value;
                    }
                    else
                    {
                        formattedMeasure.Expression = requestedMeasure.Expression; // in case of errors returns the original expression, as requested by Daniele
                        formattedMeasure.Errors = daxformatterResponse.Errors?.Select(FormatterError.CreateFrom);
                    }

                    formattedMeasures.Add(formattedMeasure);
                }
            }

            return formattedMeasures;
        }

        public DatabaseUpdateResult Update(PBIDesktopReport report, IEnumerable<FormattedMeasure> measures)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var updateResult = TabularModelHelper.Update(connection.Database, measures);

            return updateResult;
        }

        public DatabaseUpdateResult Update(PBICloudDataset dataset, IEnumerable<FormattedMeasure> measures, string accessToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(dataset, accessToken);
            var updateResult = TabularModelHelper.Update(connection.Database, measures);

            return updateResult;
        }

        private async Task<IReadOnlyList<DaxFormatterResponse>> CallDaxFormatterAsync(IEnumerable<TabularMeasure> measures, FormatDaxOptions options)
        {
            // TODO: move add/remove FormatPrefix to the Dax.Formatter NugGet package
            const string FormatPrefix = "[x] := ";
            const int FormatPrefixLength = 7;

            var request = new DaxFormatterMultipleRequest
            {
                ServerName = options.ServerName,
                ServerVersion = options.ServerVersion,
                ServerType = null, // TODO: Dax.Formatter identify ServerType
                ServerEdition = options.ServerEdition.TryParseTo<Dax.Formatter.AnalysisServices.ServerEdition>(),
                ServerMode = options.ServerMode.TryParseTo<Dax.Formatter.AnalysisServices.ServerMode>(),
                ServerLocation = options.ServerLocation.TryParseTo<Dax.Formatter.AnalysisServices.ServerLocation>(),
                DatabaseName = options.DatabaseName,
                DatabaseCompatibilityLevel = options.CompatibilityLevel is not null ? options.CompatibilityLevel.ToString() : null, // TODO: Dax.Formatter declare DatabaseCompatibilityLevel int? instead of string
                MaxLineLength = options.LineStyle,
                SkipSpaceAfterFunctionName = options.SpacingStyle,
                ListSeparator = options.ListSeparator ?? ',', // TODO: Dax.Formatter declare ListSeparator nullable
                DecimalSeparator = options.DecimalSeparator ?? '.', // TODO: Dax.Formatter declare DecimalSeparator nullable
                CallerApp = AppEnvironment.ApplicationName,
                CallerVersion = AppEnvironment.ApplicationProductVersion,
            };

            foreach (var measure in measures)
            {
                request.Dax.Add(FormatPrefix + measure.Expression);
            }

            var responses = await _daxformatterClient.FormatAsync(request).ConfigureAwait(false);

            foreach (var response in responses)
            {
                if (response.Errors is null || response.Errors.Count == 0)
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
                            // Don't 'break;' as we can have multilple errors reported for a single line
                        }
                    }
                }
            }

            return responses;
        }
    }
}
