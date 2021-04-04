using System;
using System.Data;

namespace Sqlbi.Bravo.Client.AnalysisServicesEventWatcher
{
    internal class ConnectionStateEventArgs : EventArgs
    {
        public readonly ConnectionState Previous;
        public readonly ConnectionState Current;

        public ConnectionStateEventArgs(ConnectionState previous, ConnectionState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
