using Dax.Formatter.Models;
using Serilog.Events;

namespace Sqlbi.Bravo.Core.Settings.Interfaces
{
    internal interface IDaxFormatterSettings
    {
        DaxFormatterLineStyle DaxFormatterLineStyle { get; set; }
    }
}
