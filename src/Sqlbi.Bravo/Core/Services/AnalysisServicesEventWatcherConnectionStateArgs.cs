using System;
using System.Data;

namespace Sqlbi.Bravo.Core.Services
{
    internal class AnalysisServicesEventWatcherConnectionStateArgs : EventArgs
    {
        public readonly ConnectionState Previous;
        public readonly ConnectionState Current;

        public AnalysisServicesEventWatcherConnectionStateArgs(ConnectionState previous, ConnectionState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
