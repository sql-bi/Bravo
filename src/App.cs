using Microsoft.Extensions.Hosting;
using Sqlbi.Bravo.Infrastructure;
using System;

namespace Sqlbi.Bravo
{
    internal partial class App
    {
        private readonly AppInstance _singleton;
        private static Uri? HostUri;

        public App()
        {
            _singleton = new AppInstance();
        }

        public void Run()
        {
            if (_singleton.IsOwned)
            {
                var host = CreateHost();
                _ = host.RunAsync();

                HostUri = GetHostUri(host);

                CreateWindow().WaitForClose();
            }
            else
            {
                // Another instance of the application is already running, notify the owner and exit the app gracefully
                _singleton.NotifyOwner();
            }
        }
    }
}
