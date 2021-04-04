using System;

namespace Sqlbi.Bravo.Client.AnalysisServicesEventWatcher
{
    internal class WatcherEventArgs : EventArgs
    {
        public readonly WatcherEvent Event;
        public readonly string Text;

        public WatcherEventArgs(WatcherEvent @event, string text)
        {
            Event = @event;
            Text = text;
        }
    }
}
