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
#if DEBUG
                        listenOptions.UseConnectionLogging();
#endif
                        //listenOptions.UseHttps();
                    });
                });

                webBuilder.UseStartup<Startup>();
            });

            return hostBuilder;
        }

        internal static PhotinoWindow CreateHostWindow()
        {
#if DEBUG
            var contextMenuEnabled = true;
#else
            var contextMenuEnabled = false
#endif
            // Creating a new PhotinoWindow instance with the fluent API
            var window = new PhotinoWindow()
                .SetTitle(AppConstants.ApplicationHostWindowTitle)
                .SetIconFile("wwwroot/bravo.ico")
                .SetContextMenuEnabled(contextMenuEnabled)
                .SetGrantBrowserPermissions(true)
                .SetUseOsDefaultSize(true)
                .Load("wwwroot/index.html"); // Can be used with relative path strings or "new URI()" instance to load a website.

                //.RegisterCustomSchemeHandler("app", AppCustomScheme)
                //.RegisterWindowCreatingHandler(WindowCreating)
                //.RegisterWindowCreatedHandler(WindowCreated)
                //.RegisterWindowClosingHandler(WindowClosing)
                //.RegisterWebMessageReceivedHandler(WebMessageReceived)

            return window;
        }

        public static System.IO.Stream AppCustomScheme(object sender, string scheme, string url, out string contentType)
        {
            contentType = "text/javascript";

            var script = System.Text.Encoding.UTF8.GetBytes(@"
                (() =>{
                    window.setTimeout(() => {
                        alert(`🎉 Dynamically inserted JavaScript.`);
                    }, 1000);
                })();
            ");

            return new System.IO.MemoryStream(script);
        }

        private static void WindowCreating(object? sender, EventArgs e)
        {
            //var window = (PhotinoWindow)sender;
            Console.WriteLine($"Creating new PhotinoWindow instance.");
        }

        private static void WindowCreated(object? sender, EventArgs e)
        {
            if (sender is PhotinoWindow window)
            {
                Console.WriteLine($"Created new PhotinoWindow instance with title { window.Title }.");
            }
        }

        internal static bool WindowClosing(object sender, EventArgs args)
        {
            Console.WriteLine($"Closing PhotinoWindow instance.");
            return false; // Could return true to stop windows close
        }

        internal static void WebMessageReceived(object? sender, string message)
        {
            if (sender is PhotinoWindow window)
            {
                // The message argument is coming in from sendMessage.
                // "window.external.sendMessage(message: string)"
                var response = $"Received message: \"{message}\"";

                // Send a message back the to JavaScript event handler.
                // "window.external.receiveMessage(callback: Function)"
                window.SendWebMessage(response);
            }
        }
    }
}
