using Sqlbi.Bravo.Client.AnalysisServicesEventWatcher;
using Sqlbi.Bravo.Core.Settings;
using System;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IAnalysisServicesEventWatcherService
    {
        event EventHandler<WatcherEventArgs> OnWatcherEvent;

        event EventHandler<ConnectionStateEventArgs> OnConnectionStateChanged;

        Task ConnectAsync(RuntimeSummary runtimeSummary);

        Task DisconnectAsync();
    }
}
