using Sqlbi.Bravo.Infrastructure.Configuration;
using System;

namespace Sqlbi.Bravo
{
/*
 * Toast notification 
 * https://github.com/CommunityToolkit/WindowsCommunityToolkit
 * https://docs.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/send-local-toast?tabs=desktop
 */
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                StartupConfiguration.Configure();

                var app = new App();
                app.Run();
            }
            catch (Exception)
            {
                // TODO: add logging
                throw;
            }
        }
    }
}
