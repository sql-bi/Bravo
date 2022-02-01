namespace Sqlbi.Bravo
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.ApplicationInsights;
    using Microsoft.Extensions.Logging.EventLog;
    using System;
    using System.Net;

    internal partial class Program
    {
        private static IHost CreateHost()
        {
            var hostBuilder = new HostBuilder();

            hostBuilder.UseEnvironment(Environments.Production);
            hostBuilder.UseContentRoot(Environment.CurrentDirectory);
            hostBuilder.ConfigureHostConfiguration((builder) =>
            {
                builder.SetBasePath(Environment.CurrentDirectory);
            });

            hostBuilder.ConfigureLogging((HostBuilderContext context, ILoggingBuilder logging) =>
            {
                logging.AddFilter<ApplicationInsightsLoggerProvider>((LogLevel level) => level >= LogLevel.Warning);
                logging.AddFilter<EventLogLoggerProvider>((LogLevel level) => level >= LogLevel.Warning);
                logging.AddApplicationInsights();
                logging.AddEventSourceLogger();
                logging.AddEventLog();
#if DEBUG
                logging.AddConsole();
                logging.AddDebug();
#endif
                logging.Configure((LoggerFactoryOptions options) =>
                {
                    options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId;
                });
            });

            hostBuilder.UseDefaultServiceProvider((HostBuilderContext context, ServiceProviderOptions options) =>
            {
#if DEBUG
                options.ValidateOnBuild = options.ValidateScopes = true;
#endif
            });

            hostBuilder.ConfigureWebHostDefaults((webBuilder) =>
            {
                // Empty and ignore default URLs configured on the IWebHostBuilder - this remove the warning 'Microsoft.AspNetCore.Server.Kestrel: Warning: Overriding address(es) 'https://localhost:5001/, http://localhost:5000/'. Binding to endpoints defined in UseKestrel() instead.'
                webBuilder.UseUrls();
                webBuilder.UseKestrel((serverOptions) =>
                {
#if DEBUG
                    var listenEndpoint = new IPEndPoint(IPAddress.Loopback, port: 5000);
#else
                    var listenEndpoint = new IPEndPoint(Infrastructure.Helpers.NetworkHelper.GetLoopbackAddress(), port: 0);
#endif
                    // Allow sync IO - required by ImportVpax
                    serverOptions.AllowSynchronousIO = true;
                    serverOptions.Listen(listenEndpoint, (listenOptions) =>
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
