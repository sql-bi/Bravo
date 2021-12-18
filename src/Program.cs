using Microsoft.Extensions.Hosting;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Configuration;
using System;
using System.Threading.Tasks;

namespace Sqlbi.Bravo
{
/*
 * Toast notification 
 * https://github.com/CommunityToolkit/WindowsCommunityToolkit
 * https://docs.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/send-local-toast?tabs=desktop
 */
    internal partial class Program
    {
        private static readonly AppInstance _singleton = new();
        private static Uri? _hostUri;

        [STAThread]
        public static void Main()
        {
            try
            {
                StartupConfiguration.Configure();

                if (_singleton.IsOwned == false)
                {
                    // Another instance of the application is already running, notify the owner and exit gracefully
                    _singleton.NotifyOwner();
                    return;
                }
                
                using var host = CreateHost();

                host.Start();
                {
                    _hostUri = GetUri(host);
                    CreateWindow().WaitForClose(); // Starts the native main window that runs the message loop
                }
                host.StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // TODO: add logging
                throw;
            }
        }
    }
}
