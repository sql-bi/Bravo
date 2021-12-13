using System;
using System.Linq;
using TOM = Microsoft.AnalysisServices;

namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    public static class XmlaExtensions
    {
        public static string ToDescriptionString(this TOM.XmlaResultCollection results)
        {
            var descriptions = results.OfType<TOM.XmlaResult>()
                .SelectMany((r) => r.Messages.OfType<TOM.XmlaMessage>())
                .Select((m) => m.Description)
                .ToArray();

            return string.Join(Environment.NewLine, descriptions);
        }
    }
}
