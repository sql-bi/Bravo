using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using PhotinoNET;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Configuration;
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
            try
            {
                using var instance = new AppInstance();

                if (instance.IsOwned)
                {
                    StartupConfiguration.Configure();
                    CreateHost().RunAsync();

                    var window = CreateWindow();

                    window.WindowCreated += (sender, e) => instance.TryHook(sender);

                    window.WaitForClose();
                }
            }
            catch (Exception)
            {
                // TODO: add logging
                throw;
            }
        }

        private static IHost CreateHost()
        {
            var hostBuilder = new HostBuilder();

            hostBuilder.UseContentRoot(Directory.GetCurrentDirectory());

            hostBuilder.ConfigureHostConfiguration((builder) =>
            {
                builder.SetBasePath(Directory.GetCurrentDirectory());
                //builder.AddJsonFile("hostsettings.json", optional: true);
                //builder.AddEnvironmentVariables(prefix: "CUSTOMPREFIX_");
                //builder.AddCommandLine(args);
            });

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

            return hostBuilder.Build();
        }

        private static PhotinoWindow CreateWindow()
        {
#if DEBUG
            var contextMenuEnabled = true;
#else
            var contextMenuEnabled = false;
#endif
            var window = new PhotinoWindow()
                .SetTitle(AppConstants.ApplicationMainWindowTitle)
                .SetIconFile("wwwroot/bravo.ico")
                .SetContextMenuEnabled(contextMenuEnabled)
                .SetGrantBrowserPermissions(true)
                .SetUseOsDefaultSize(true)
                .RegisterWebMessageReceivedHandler(WindowWebMessageReceived)
                .Load("wwwroot/index.html");

            window.RegisterWebMessageReceivedHandler(WindowWebMessageReceived);
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
            
            window.SendNotification("::Bravo:INF:WindowCreatedHandler", window.Title);
        }

        private static bool WindowClosing(object sender, EventArgs args)
        {
            Trace.WriteLine($"::Bravo:INF:WindowClosingHandler");
            return false; // Could return true to stop windows close
        }

        private static void WindowWebMessageReceived(object? sender, string message)
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
