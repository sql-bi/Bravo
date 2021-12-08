using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using PhotinoNET;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Windows;
using System;
using System.Diagnostics;

namespace Sqlbi.Bravo
{
    public class Program
    {
        internal static PhotinoWindow? HostWindow;

        [STAThread]
        public static void Main(string[] args)
        {
            NativeMethods.SetProcessDPIAware();

            // Connect API
            _ = CreateHostBuilder(args).Build().RunAsync();

            // Starts the application event loop
            CreateHostWindow().WaitForClose();
        }

        internal static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults((webBuilder) =>
            {
                Trace.WriteLine("CreateHostBuilder");

                //webBuilder.ConfigureLogging(builder =>
                //{
                //    builder.
                //});

                webBuilder.ConfigureKestrel(options =>
                {
                    // Allow sync IO - required by ImportVpax
                    options.AllowSynchronousIO = true;
                    // TODO: randomise the HTTP listening port
                    options.ListenLocalhost(port: 5000, (listenOptions) =>
                    {
                        listenOptions.UseConnectionLogging();
                        //listenOptions.UseHttps();
                    });
                });

                webBuilder.UseStartup<Startup>();
            });

            return hostBuilder;
        }

        internal static PhotinoWindow CreateHostWindow()
        {
            // Creating a new PhotinoWindow instance with the fluent API
            var window = new PhotinoWindow()
                .SetTitle(AppConstants.ApplicationHostWindowTitle)
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
            HostWindow = window;

            return window;
        }
    }
}
