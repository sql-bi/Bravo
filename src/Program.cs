using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using PhotinoNET;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo
{

    static class NativeMethods {[DllImport("user32.dll", SetLastError = true)] public static extern bool SetProcessDPIAware(); }


    class Program
    {



        [STAThread]
        static void Main(string[] args)
        {

            NativeMethods.SetProcessDPIAware();
  

            // Connect API
            CreateHostBuilder(args).Build().RunAsync();

            // Window title declared here for visibility
            string windowTitle = "Bravo for Power BI";

            // Creating a new PhotinoWindow instance with the fluent API
            var window = new PhotinoWindow()
                .SetTitle(windowTitle)
                .SetIconFile("wwwroot/bravo.ico")
                .SetGrantBrowserPermissions(true)
                .SetUseOsDefaultSize(true)
                //.SetContextMenuEnabled(false)

/*
                .RegisterCustomSchemeHandler("app", (object sender, string scheme, string url, out string contentType) =>
                {
                    contentType = "text/javascript";
                    return new MemoryStream(Encoding.UTF8.GetBytes(@"
                        (() =>{
                            window.setTimeout(() => {
                                alert(`🎉 Dynamically inserted JavaScript.`);
                            }, 1000);
                        })();
                    "));
                })
*/
                .RegisterWindowCreatingHandler((object sender, EventArgs args) =>
                {
                    var window = (PhotinoWindow)sender;
                    Console.WriteLine($"Creating new PhotinoWindow instance.");
                })
                .RegisterWindowCreatedHandler((object sender, EventArgs args) =>
                {
                    var window = (PhotinoWindow)sender;
                    Console.WriteLine($"Created new PhotinoWindow instance with title {window.Title}.");
                })
                .RegisterWindowClosingHandler((object sender, EventArgs args) =>
                {
                    var window = (PhotinoWindow)sender;
                    Console.WriteLine($"Closing PhotinoWindow instance.");
                    return false; // Could return true to stop windows close
                })
                // Most event handlers can be registered after the
                // PhotinoWindow was instantiated by calling a registration 
                // method like the following RegisterWebMessageReceivedHandler.
                // This could be added in the PhotinoWindowOptions if preferred.
                .RegisterWebMessageReceivedHandler((object sender, string message) =>
                {
                    var window = (PhotinoWindow)sender;

                    // The message argument is coming in from sendMessage.
                    // "window.external.sendMessage(message: string)"
                    string response = $"Received message: \"{message}\"";

                    // Send a message back the to JavaScript event handler.
                    // "window.external.receiveMessage(callback: Function)"
                    window.SendWebMessage(response);
                })
                .Load("wwwroot/index.html"); // Can be used with relative path strings or "new URI()" instance to load a website.

            // Pass the main window to HostWindow controller (not compatible with multi-window)
            Controllers.HomeController.HostWindow = window;

            window.WaitForClose(); // Starts the application event loop
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        Trace.WriteLine("CreateHostBuilder");
        //webBuilder.ConfigureLogging(builder =>
        //{
        //    builder.
        //});

        //webBuilder.ConfigureKestrel(options =>
        //{
        //    options.
        //});

        webBuilder.UseStartup<Startup>();
   
    });

    }
}
