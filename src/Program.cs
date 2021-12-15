using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using PhotinoNET;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Windows.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Sqlbi.Bravo
{
    public class Program
    {
        internal static ICollection<string>? HostAddresses;

        [STAThread]
        public static void Main()
        {
            NativeMethods.SetProcessDPIAware();

            // Connect API
            CreateHostBuilder().Build().RunAsync();

            // Starts the application event loop
            CreateHostWindow().WaitForClose();
        }

        private static IHostBuilder CreateHostBuilder(string[]? args = null)
        {
            var hostBuilder = new HostBuilder();

            hostBuilder.UseContentRoot(Directory.GetCurrentDirectory());

            //hostBuilder.ConfigureHostConfiguration((config) =>
            //{
            //    config.AddEnvironmentVariables("DOTNET_");
            //    if (args != null)
            //        config.AddCommandLine(args);
            //});

            hostBuilder.ConfigureAppConfiguration((HostBuilderContext hostingContext, IConfigurationBuilder config) =>
            {
                var hostingEnvironment = hostingContext.HostingEnvironment;
                var reloadConfigOnChange = hostingContext.Configuration.GetValue("hostBuilder:reloadConfigOnChange", defaultValue: true);

                config.AddJsonFile($"appsettings.json", optional: true, reloadConfigOnChange);
                config.AddJsonFile($"appsettings.{ hostingEnvironment.EnvironmentName }.json", optional: true, reloadConfigOnChange);

                //if (hostingEnvironment.IsDevelopment() && !string.IsNullOrEmpty(hostingEnvironment.ApplicationName))
                //{
                //    var assembly = Assembly.Load(new AssemblyName(hostingEnvironment.ApplicationName));
                //    if (assembly != null)
                //        config.AddUserSecrets(assembly, optional: true);
                //}

                //config.AddEnvironmentVariables();

                //if (args != null)
                //    config.AddCommandLine(args);
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

            hostBuilder.UseDefaultServiceProvider((HostBuilderContext context, ServiceProviderOptions options) =>
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

                webBuilder.ConfigureKestrel((options) =>
                {
                    // Allow sync IO - required by ImportVpax
                    options.AllowSynchronousIO = true;
                    // TODO: randomise the listening port 
                    options.ListenLocalhost(port: 5000, (listenOptions) =>
                    //options.Listen(System.Net.IPAddress.Loopback, port: 0, (listenOptions) =>
                    {
#if DEBUG
                        listenOptions.UseConnectionLogging();
#endif
                        //listenOptions.UseHttps(); // TODO: do we need https ?
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
            var contextMenuEnabled = false;
#endif
            // Creating a new PhotinoWindow instance with the fluent API
            var window = new PhotinoWindow()
                .SetTitle(AppConstants.ApplicationHostWindowTitle)
                .SetIconFile("wwwroot/bravo.ico")
                .SetContextMenuEnabled(contextMenuEnabled)
                .SetGrantBrowserPermissions(true)
                .SetUseOsDefaultSize(true)
                .RegisterWebMessageReceivedHandler(WebMessageReceived)
                .Load("wwwroot/index.html"); // Can be used with relative path strings or "new URI()" instance to load a website.

            window.RegisterWebMessageReceivedHandler(WebMessageReceived);
            window.RegisterWindowCreatingHandler(WindowCreating);
            window.RegisterWindowCreatedHandler(WindowCreated);
            window.RegisterWindowClosingHandler(WindowClosing);

            return window;
        }

        private static void WindowCreating(object? sender, EventArgs e)
        {
            var window = (PhotinoWindow)sender!;
            Trace.WriteLine($"::Bravo:INF:WindowCreatingHandler:{ window.Title }");
        }

        private static void WindowCreated(object? sender, EventArgs e)
        {
            var window = (PhotinoWindow)sender!;
            Trace.WriteLine($"::Bravo:INF:WindowCreatedHandler:{ window.Title }");
            Trace.WriteLine($"::Bravo:INF:HostAddresses:{ string.Join(", ", HostAddresses ?? Array.Empty<string>()) }");
        }

        private static bool WindowClosing(object sender, EventArgs args)
        {
            Trace.WriteLine($"::Bravo:INF:WindowClosingHandler");
            return false; // Could return true to stop windows close
        }

        private static void WebMessageReceived(object? sender, string message)
        {
            var window = (PhotinoWindow)sender!;

            if (message == "host-address")
            {
                Trace.WriteLine($"::Bravo:INF:WebMessageReceived:{ message }");

                var address = HostAddresses?.Single();

                window.SendWebMessage(address);
            }
            else
            {
                // ??
            }
        }
    }
}
