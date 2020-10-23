using System;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IApplicationInstanceService
    {
        public void RegisterCallbackForMultipleInstanceStarted(Action<IntPtr> callback);

        bool IsCurrentInstanceOwned { get; }
    }
}
