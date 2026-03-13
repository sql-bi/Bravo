namespace Sqlbi.Bravo
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.EventLog;
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

            hostBuilder.ConfigureLogging((context, logging) =>
            {
                logging.AddFilter<EventLogLoggerProvider>((level) => level >= LogLevel.Warning);
                logging.AddEventSourceLogger();
                logging.AddEventLog();
#if DEBUG
                logging.AddConsole();
                logging.AddDebug();
#endif
                logging.Configure((options) =>
                {
                    options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId;
                });
            });

            hostBuilder.UseDefaultServiceProvider((context, options) =>
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
                    const int port = 5000;
#else
                    const int port = 0; // Use dynamic port assignment
#endif
                    serverOptions.Listen(new IPEndPoint(IPAddress.Loopback, port));
                    serverOptions.AllowSynchronousIO = true; // required by ImportVpax
                });

                webBuilder.UseStartup<Startup>();
            });

            var host = hostBuilder.Build();
            return host;
        }
    }
}
