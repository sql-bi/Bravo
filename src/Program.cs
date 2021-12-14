using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using PhotinoNET;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Windows;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            NativeMethods.SetProcessDPIAware();

            // Connect API
            _ = CreateHostBuilder().Build().RunAsync();

            // Starts the application event loop
            CreateHostWindow().WaitForClose();
        }

        private static IHostBuilder CreateHostBuilder(string[]? args = null)
        {
            var hostBuilder = new HostBuilder();

            hostBuilder.UseContentRoot(Directory.GetCurrentDirectory());

            hostBuilder.ConfigureHostConfiguration((config) =>
            {
                config.AddEnvironmentVariables("DOTNET_");
                if (args != null)
                    config.AddCommandLine(args);
            });

            hostBuilder.ConfigureAppConfiguration((HostBuilderContext hostingContext, IConfigurationBuilder config) =>
            {
                var hostingEnvironment = hostingContext.HostingEnvironment;
                var reloadConfigOnChange = hostingContext.Configuration.GetValue("hostBuilder:reloadConfigOnChange", defaultValue: true);

                config.AddJsonFile($"appsettings.json", optional: true, reloadConfigOnChange);
                config.AddJsonFile($"appsettings.{ hostingEnvironment.EnvironmentName }.json", optional: true, reloadConfigOnChange);

                if (hostingEnvironment.IsDevelopment() && !string.IsNullOrEmpty(hostingEnvironment.ApplicationName))
                {
                    var assembly = Assembly.Load(new AssemblyName(hostingEnvironment.ApplicationName));
                    if (assembly != null)
                        config.AddUserSecrets(assembly, optional: true);
                }

                config.AddEnvironmentVariables();

                if (args != null)
                    config.AddCommandLine(args);
            });

            hostBuilder.ConfigureLogging((HostBuilderContext hostingContext, ILoggingBuilder logging) =>
            {
                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                if (isWindows)
                    logging.AddFilter<EventLogLoggerProvider>((LogLevel level) => level >= LogLevel.Warning);

                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();

                if (isWindows)
                    logging.AddEventLog();

                logging.Configure((LoggerFactoryOptions options) =>
                {
                    options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId;
                });
            });

            hostBuilder.UseDefaultServiceProvider(delegate (HostBuilderContext context, ServiceProviderOptions options)
            {
                var validateOnBuild = (options.ValidateScopes = context.HostingEnvironment.IsDevelopment());
                options.ValidateOnBuild = validateOnBuild;
            });

            hostBuilder.ConfigureWebHostDefaults((webBuilder) =>
            {
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

        private static PhotinoWindow CreateHostWindow()
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

        private static System.IO.Stream AppCustomScheme(object sender, string scheme, string url, out string contentType)
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

        private static bool WindowClosing(object sender, EventArgs args)
        {
            Console.WriteLine($"Closing PhotinoWindow instance.");
            return false; // Could return true to stop windows close
        }

        private static void WebMessageReceived(object? sender, string message)
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
