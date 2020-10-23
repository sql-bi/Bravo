using Serilog.Core;
using Serilog.Events;
using System.Threading;

namespace Sqlbi.Bravo.Core.Logging
{
    internal class ManagedThreadEnricher : ILogEventEnricher
    {
        public const string ManagedThreadPropertyName = "ManagedThread";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var info = $"thread:{ Thread.CurrentThread.ManagedThreadId }";

            var threadName = Thread.CurrentThread.Name;
            if (threadName != null)
                info += $"<{ threadName }>";

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(ManagedThreadPropertyName, info));
        }
    }
}
