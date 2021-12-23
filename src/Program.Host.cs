using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System;
using System.IO;

namespace Sqlbi.Bravo
{
    internal partial class Program
    {
        private static IHost CreateHost()
        {
            var hostBuilder = new HostBuilder();

            hostBuilder.UseContentRoot(AppContext.BaseDirectory);

            hostBuilder.ConfigureHostConfiguration((builder) =>
            {
                builder.SetBasePath(AppContext.BaseDirectory);
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
#if DEBUG
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

                webBuilder.ConfigureKestrel((options) =>
                {
                    // Allow sync IO - required by ImportVpax
                    options.AllowSynchronousIO = true;
                    
                    // TODO: randomise the listening port 
                    //var listenAddress = NetworkHelper.GetLoopbackAddress();
                    //options.Listen(listenAddress, port: 0, (listenOptions) =>
                    options.ListenLocalhost(port: 5000, (listenOptions) =>
                    {
#if DEBUG
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
