using System;

namespace Sqlbi.Bravo.Core.Services
{
    internal class AnalysisServicesEventWatcherEventArgs : EventArgs
    {
        public readonly AnalysisServicesEventWatcherEvent Event;
        public readonly string Text;

        public AnalysisServicesEventWatcherEventArgs(AnalysisServicesEventWatcherEvent @event, string text)
        {
            Event = @event;
            Text = text;
        }
    }
}
