using System;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IApplicationInstanceService
    {
        void NotifyConnectionToPrimaryInstance();

        (string ConnectionName, string ServerName, string DatabaseName) ReceiveConnectionFromSecondaryInstance(IntPtr ptr);

        void RegisterCallbackForMultipleInstanceStarted(Action<IntPtr> callback);

        bool IsCurrentInstanceOwned { get; }
    }
}