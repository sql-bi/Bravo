using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System;
using System.Net;

namespace Sqlbi.Bravo
{
    internal partial class Program
    {
        private static IHost CreateHost()
        {
            var hostBuilder = new HostBuilder();

            hostBuilder.UseContentRoot(Environment.CurrentDirectory);

            hostBuilder.ConfigureHostConfiguration((builder) =>
            {
                builder.SetBasePath(Environment.CurrentDirectory);
                //builder.AddJsonFile("hostsettings.json", optional: true);
                //builder.AddEnvironmentVariables(prefix: "CUSTOMPREFIX_");
                //builder.AddCommandLine(args);
            });

            hostBuilder.ConfigureAppConfiguration((HostBuilderContext hostingContext, IConfigurationBuilder config) =>
            {
                //var hostingEnvironment = hostingContext.HostingEnvironment;
                //var reloadConfigOnChange = hostingContext.Configuration.GetValue("hostBuilder:reloadConfigOnChange", defaultValue: true);

                // TODO: rename and move to user-settings file/folder
                config.AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true);
                //config.AddJsonFile($"appsettings.{ hostingEnvironment.EnvironmentName }.json", optional: true, reloadConfigOnChange);

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
                logging.AddFilter<EventLogLoggerProvider>((LogLevel level) => level >= LogLevel.Warning);
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
#if DEBUG || DEBUG_WWWROOT
                logging.AddConsole();
                logging.AddDebug();
#endif
                logging.AddEventSourceLogger();
                logging.AddEventLog();

                logging.Configure((LoggerFactoryOptions options) =>
                {
                    options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId;
                });
            });

            hostBuilder.UseDefaultServiceProvider((HostBuilderContext context, ServiceProviderOptions options) =>
            {
                options.ValidateOnBuild = (options.ValidateScopes = context.HostingEnvironment.IsDevelopment());
            });

            hostBuilder.ConfigureWebHostDefaults((webBuilder) =>
            {
                //webBuilder.ConfigureLogging(builder =>
                //{
                //    builder.
                //});

                webBuilder.ConfigureKestrel((serverOptions) =>
                {
#if DEBUG || DEBUG_WWWROOT
                    var listenEndpoint = new IPEndPoint(IPAddress.Loopback, port: 5000);
#else
                    var listenEndpoint = new IPEndPoint(Infrastructure.Helpers.NetworkHelper.GetLoopbackAddress(), port: 0);
#endif
                    // Allow sync IO - required by ImportVpax
                    serverOptions.AllowSynchronousIO = true;
                    serverOptions.Listen(listenEndpoint, (listenOptions) =>
                    {
#if DEBUG || DEBUG_WWWROOT
                        listenOptions.UseConnectionLogging();
#endif
                        //listenOptions.UseHttps(); // TODO: do we need https ?
                    });
                });
                
                webBuilder.UseStartup<Startup>();
            });

            var host = hostBuilder.Build();
            return host;
        }
    }
}
