using Sqlbi.Bravo.Core.Settings;
using System;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IApplicationInstanceService
    {
        void NotifyConnectionToPrimaryInstance();

        RuntimeSummary ReceiveConnectionFromSecondaryInstance(IntPtr ptr);

        void RegisterCallbackForMultipleInstanceStarted(Action<IntPtr> callback);

        bool IsCurrentInstanceOwned { get; }
    }
}
